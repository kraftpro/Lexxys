#if NET
using System.Net.Http.Headers;
using System.Text;
using static Lexxys.ZenJson;

namespace Lexxys.Adobee;

/// <summary>
/// Minimal Adobe Sign API client for embedded PDF signing flow.
/// </summary>
public sealed class AdobeeSignClient: IDisposable
{
	private const string DefaultApiBaseUrl = "https://api.na1.adobesign.com";
	private const string DefaultImsTokenUrl = "https://ims-na1.adobelogin.com/ims/token/v3";
	private const string DefaultScope = "agreement_read agreement_write agreement_send user_login:self";

	private readonly HttpClient _httpClient;
	private readonly bool _ownsHttpClient;
	private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

	private string? _cachedAccessToken;
	private DateTime _accessTokenExpiresUtc;

	public AdobeeSignClient(string apiKey, string apiSecret, HttpClient? httpClient = null, string? apiBaseUrl = null, string? imsTokenUrl = null)
	{
		if (String.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
		if (String.IsNullOrWhiteSpace(apiSecret)) throw new ArgumentException("API secret cannot be null or empty.", nameof(apiSecret));

		ApiKey = apiKey;
		ApiSecret = apiSecret;
		ApiBaseUrl = String.IsNullOrWhiteSpace(apiBaseUrl) ? DefaultApiBaseUrl: apiBaseUrl;
		ImsTokenUrl = String.IsNullOrWhiteSpace(imsTokenUrl) ? DefaultImsTokenUrl: imsTokenUrl;

		_httpClient = httpClient ?? new HttpClient();
		_ownsHttpClient = httpClient is null;
	}

	public string ApiKey { get; }
	public string ApiSecret { get; }
	public string ApiBaseUrl { get; }
	public string ImsTokenUrl { get; }

	/// <summary>
	/// Optional API user context header ("email:user@domain.com" or "group:...").
	/// </summary>
	public string? ApiUser { get; set; }

	/// <summary>
	/// OAuth scope used to request tokens.
	/// </summary>
	public string OAuthScope { get; set; } = DefaultScope;

	/// <summary>
	/// Uploads PDF, creates agreement and returns the signer URL suitable for embedding.
	/// </summary>
	public async Task<AdobeeEmbeddedSigningSession> CreateEmbeddedSigningSessionAsync(
		Stream pdf,
		string fileName,
		string signerEmail,
		string? signerName = null,
		string? agreementName = null,
		int signingUrlAttempts = 10,
		CancellationToken cancellationToken = default)
	{
		if (pdf is null) throw new ArgumentNullException(nameof(pdf));
		if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty.", nameof(fileName));
		if (String.IsNullOrWhiteSpace(signerEmail)) throw new ArgumentException("Signer email cannot be empty.", nameof(signerEmail));

		var transientDocumentId = await UploadTransientDocumentAsync(pdf, fileName, cancellationToken).ConfigureAwait(false);
		var agreementId = await CreateAgreementForEmbeddedSigningAsync(
			transientDocumentId,
			signerEmail,
			signerName,
			agreementName ?? fileName,
			cancellationToken).ConfigureAwait(false);
		var signingUrl = await GetEmbeddedSigningUrlAsync(agreementId, signingUrlAttempts, cancellationToken).ConfigureAwait(false);

		return new AdobeeEmbeddedSigningSession(agreementId, transientDocumentId, signingUrl);
	}

	public async Task<string> UploadTransientDocumentAsync(Stream pdf, string fileName, CancellationToken cancellationToken = default)
	{
		if (pdf is null) throw new ArgumentNullException(nameof(pdf));
		if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty.", nameof(fileName));

		var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{ApiBaseUrl}/api/rest/v6/transientDocuments", cancellationToken).ConfigureAwait(false);
		using var form = new MultipartFormDataContent();
		var streamContent = new StreamContent(pdf);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
		form.Add(streamContent, "File", fileName);
		form.Add(new StringContent(fileName, Encoding.UTF8), "File-Name");
		form.Add(new StringContent("application/pdf", Encoding.UTF8), "Mime-Type");
		request.Content = form;

		var body = await SendAndReadBodyAsync(request, cancellationToken).ConfigureAwait(false);
		return GetRequiredString(body, "transientDocumentId");
	}

	public async Task<string> CreateAgreementForEmbeddedSigningAsync(
		string transientDocumentId,
		string signerEmail,
		string? signerName,
		string agreementName,
		CancellationToken cancellationToken = default)
	{
		if (String.IsNullOrWhiteSpace(transientDocumentId)) throw new ArgumentException("Transient document id cannot be empty.", nameof(transientDocumentId));
		if (String.IsNullOrWhiteSpace(signerEmail)) throw new ArgumentException("Signer email cannot be empty.", nameof(signerEmail));
		if (String.IsNullOrWhiteSpace(agreementName)) throw new ArgumentException("Agreement name cannot be empty.", nameof(agreementName));


		JsonMap payload = J(
			("fileInfos", J(
					("transientDocumentId", J(transientDocumentId)))),
			("name", J(agreementName)),
			("participantSetsInfo", J(
				J(
					("memberInfos", J(
						J(
							("email", J(signerEmail)),
							("name", J(String.IsNullOrWhiteSpace(signerName) ? signerEmail: signerName))
						))),
					("order", J(1)),
					("role", J("SIGNER")))
				)),
			("signatureType", J("ESIGN")),
			("state", J("IN_PROCESS")));

		/*
		var payload = new JsonMap(
		[
			new JsonPair("fileInfos", new JsonArray(
			[
				new JsonMap(
				[
					new JsonPair("transientDocumentId", new JsonScalar(transientDocumentId)),
				])
			])),
			new JsonPair("name", new JsonScalar(agreementName)),
			new JsonPair("participantSetsInfo", new JsonArray(
			[
				new JsonMap(
				[
					new JsonPair("memberInfos", new JsonArray(
					[
						new JsonMap(
						[
							new JsonPair("email", new JsonScalar(signerEmail)),
							new JsonPair("name", new JsonScalar(String.IsNullOrWhiteSpace(signerName) ? signerEmail: signerName!)),
						]),
					])),
					new JsonPair("order", new JsonScalar(1)),
					new JsonPair("role", new JsonScalar("SIGNER")),
				]),
			])),
			new JsonPair("signatureType", new JsonScalar("ESIGN")),
			new JsonPair("state", new JsonScalar("IN_PROCESS")),
		]);
		*/

		var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, $"{ApiBaseUrl}/api/rest/v6/agreements", cancellationToken).ConfigureAwait(false);
		request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

		var body = await SendAndReadBodyAsync(request, cancellationToken).ConfigureAwait(false);
		return GetRequiredString(body, "id");
	}

	public async Task<string> GetEmbeddedSigningUrlAsync(
		string agreementId,
		int maxAttempts = 10,
		CancellationToken cancellationToken = default)
	{
		if (String.IsNullOrWhiteSpace(agreementId)) throw new ArgumentException("Agreement id cannot be empty.", nameof(agreementId));
		if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

		string? responseBody = null;
		for (int i = 0; ;)
		{
			var request = await CreateAuthorizedRequestAsync(
				HttpMethod.Get,
				$"{ApiBaseUrl}/api/rest/v6/agreements/{Uri.EscapeDataString(agreementId)}/signingUrls",
				cancellationToken
				).ConfigureAwait(false);

			responseBody = await SendAndReadBodyAsync(request, cancellationToken).ConfigureAwait(false);
			if (TryGetFirstStringValue(responseBody, out var url, "esignUrl", "url"))
				return url;

			if (++i >= maxAttempts)
				throw new InvalidOperationException($"Embedded signing URL was not returned for agreement '{agreementId}'. Last response: {Shorten(responseBody)}");

			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
		}

	}

	public void SetAccessToken(string accessToken, DateTime expiresUtc)
	{
		if (String.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));

		_cachedAccessToken = accessToken;
		_accessTokenExpiresUtc = expiresUtc;
	}

	public void Dispose()
	{
		_tokenLock.Dispose();
		if (_ownsHttpClient)
			_httpClient.Dispose();
	}

	private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
	{
		var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
		var request = new HttpRequestMessage(method, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		if (!String.IsNullOrWhiteSpace(ApiUser))
			request.Headers.Add("x-api-user", ApiUser);
		return request;
	}

	private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
	{
		if (!String.IsNullOrWhiteSpace(_cachedAccessToken) && _accessTokenExpiresUtc > DateTime.UtcNow.AddSeconds(30))
			return _cachedAccessToken;

		await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (!String.IsNullOrWhiteSpace(_cachedAccessToken) && _accessTokenExpiresUtc > DateTime.UtcNow.AddSeconds(30))
				return _cachedAccessToken;

			using var request = new HttpRequestMessage(HttpMethod.Post, ImsTokenUrl);
			request.Content = new FormUrlEncodedContent(
			[
				KeyValuePair.Create("grant_type", "client_credentials"),
				KeyValuePair.Create("client_id", ApiKey),
				KeyValuePair.Create("client_secret", ApiSecret),
				KeyValuePair.Create("scope", OAuthScope),
			]);

			var body = await SendAndReadBodyAsync(request, cancellationToken).ConfigureAwait(false);
			var token = GetRequiredString(body, "access_token");
			var expiresIn = TryGetInt(body, "expires_in", out var seconds) ? Math.Max(30, seconds): 3600;

			_cachedAccessToken = token;
			_accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn - 30);
			return token;
		}
		finally
		{
			_tokenLock.Release();
		}
	}

	private async Task<string> SendAndReadBodyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var body = response.Content == null ? string.Empty: await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			throw new HttpRequestException($"Request to '{request.RequestUri}' failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Shorten(body)}");

		return body;
	}

	private static string GetRequiredString(string json, string key)
	{
		if (TryGetFirstStringValue(json, out var value, key))
			return value;
		throw new InvalidOperationException($"Response does not contain '{key}'. Response: {Shorten(json)}");
	}

	private static bool TryGetFirstStringValue(string json, out string value, params string[] keys)
	{
		value = string.Empty;
		if (string.IsNullOrWhiteSpace(json) || keys == null || keys.Length == 0)
			return false;
			
		var jsonResult = JsonParser.Parse(json);
		if (!jsonResult)
			return false;

		JsonItem root = jsonResult.Value;
		foreach (var key in keys)
		{
			if (TryFindString(root, key, out value!) && value.Length > 0)
				return true;
		}
		return false;
	}

	private static bool TryGetInt(string json, string key, out int value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(json))
			return false;
		JsonItem root;
		try
		{
			var jsonResult = JsonParser.Parse(json);
			if (!jsonResult)
				return false;
			root = jsonResult.Value;
		}
		catch
		{
			return false;
		}
		return TryFindInt(root, key, out value);
	}

	private static bool TryFindValue(JsonItem json, string key, [MaybeNullWhen(false)] out JsonScalar value)
	{
		value = default;
		if (json is JsonMap map)
		{
			foreach (var pair in map)
			{
				if (pair.IsEmpty)
					continue;

				if (pair.Name == key && pair.Item is JsonScalar scalar && scalar.Value is not null)
				{
					value = scalar;
					return true;
				}
				if (TryFindValue(pair.Item, key, out value))
					return true;
			}
		}
		else if (json is JsonArray array)
		{
			foreach (var item in array)
			{
				if (TryFindValue(item, key, out value))
					return true;
			}
		}
		return false;
	}

	private static bool TryFindValue<T>(JsonItem json, string key, Func<JsonScalar, T> converter, [MaybeNullWhen(false)] out T value)
	{
		value = default;
		if (json is JsonMap map)
		{
			foreach (var pair in map)
			{
				if (pair.IsEmpty)
					continue;

				if (pair.Name == key && pair.Item is JsonScalar scalar && scalar.Value is not null)
				{
					value = converter(scalar);
					return true;
				}
				if (TryFindValue(pair.Item, key, converter, out value))
					return true;
			}
		}
		else if (json is JsonArray array)
		{
			foreach (var item in array)
			{
				if (TryFindValue(item, key, converter, out value))
					return true;
			}
		}
		return false;
	}

	private static bool TryFindString(JsonItem item, string key, [MaybeNullWhen(false)] out string value) => TryFindValue(item, key, o => o.StringValue, out value);

	private static bool TryFindInt(JsonItem item, string key, out int value) => TryFindValue(item, key, o => o.IntValue, out value);

	private static string Shorten(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "<empty>";
		const int limit = 400;
		return value.Length <= limit ? value: String.Concat(value.AsSpan(0, limit), "...");
	}
}

public sealed class AdobeeEmbeddedSigningSession(string agreementId, string transientDocumentId, string signingUrl)
{
	public string AgreementId { get; } = agreementId ?? throw new ArgumentNullException(nameof(agreementId));
	public string TransientDocumentId { get; } = transientDocumentId ?? throw new ArgumentNullException(nameof(transientDocumentId));
	public string SigningUrl { get; } = signingUrl ?? throw new ArgumentNullException(nameof(signingUrl));
}

#endif