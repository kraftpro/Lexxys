// Lexxys Infrastructural library.
// file: ConfigurationException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Runtime.Serialization;

namespace Lexxys;

using Xml;

[Serializable]
public class ConfigurationException: InvalidOperationException
{
	public const string DicNodeName = "Node";

	public ConfigurationException()
	{
	}

	public ConfigurationException(XmlLiteNode? config)
	{
		if (config != null)
			base.Data[DicNodeName] = config.ToString();
	}

	public ConfigurationException(string? message)
		: base(message)
	{
	}

	public ConfigurationException(string key, Type? type)
		: base(SR.ConfigValueNotFound(key, type))
	{
	}

	public ConfigurationException(string? message, XmlLiteNode? config)
		: base(message)
	{
		if (config != null)
			base.Data[DicNodeName] = config.ToString();
	}

	public ConfigurationException(string? message, Exception? exception)
		: base(message, exception)
	{
	}

	public ConfigurationException(string? message, XmlLiteNode? config, Exception? exception)
		: base(message, exception)
	{
		if (config != null)
			base.Data[DicNodeName] = config.ToString();
	}

	protected ConfigurationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}


