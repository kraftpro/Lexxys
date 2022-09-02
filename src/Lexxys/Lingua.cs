// Lexxys Infrastructural library.
// file: Lingua.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using Lexxys;

namespace Lexxys
{
	public static class Lingua
	{
		public static string Plural(string text)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return String.IsNullOrWhiteSpace(text) ? text: PluralRules.Plural(text, false);
		}

		public static string Plural(string text, int count)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return String.IsNullOrWhiteSpace(text) ? text: PluralRules.Plural(text, false);
		}

		public static string Plural(string text, bool classicalEnglish)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return String.IsNullOrWhiteSpace(text) ? text: PluralRules.Plural(text, classicalEnglish);
		}

		public static string Singular(string text)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return String.IsNullOrWhiteSpace(text) ? text: PluralRules.Singular(text, false);
		}

		public static string Singular(string text, bool classicalEnglish)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			return String.IsNullOrWhiteSpace(text) ? text: PluralRules.Singular(text, classicalEnglish);
		}

		public static string Ord(long value)
		{
			return NumberRules.Ord(value);
		}

		public static string Ord(decimal value)
		{
			return NumberRules.Ord(value);
		}

		public static string Ord(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			return NumberRules.Ord(value);
		}

		public static string OrdPostfix(long value)
		{
			return NumberRules.GetOrdinalPostfix(value);
		}

		public static string NumWord(string value, string? comma = null, string? and = null)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var text = new StringBuilder();
			foreach(var item in DigitsRex.Split(value))
			{
				if (item.Length > 0)
					text.Append(Char.IsDigit(item, 0) ? NumberRules.NumWord(item, comma, and): item);
			}
			return text.ToString();
		}
		private static readonly Regex DigitsRex = new Regex(@"(\d+(?:,\d+)*)");

		public static string NumWord(decimal value, string? comma = null, string? and = null)
		{
			return NumberRules.NumWord(value, comma, and);
		}

		private static string SetCase(string value, string original)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (original == null || original.Length == 0)
				throw new ArgumentNullException(nameof(original));

			if (!Char.IsUpper(original[0]))
				return value;
			if (original.Length == 1)
				return original == "I" ? value: CultureInfo.CurrentCulture.TextInfo.ToUpper(value);
			if (Char.IsUpper(original[1]))
				return CultureInfo.CurrentCulture.TextInfo.ToUpper(value);
			return CultureInfo.CurrentCulture.TextInfo.ToUpper(value[0]).ToString() + value.Substring(1);
		}

		/// <summary>
		/// English Plural Rules (based on Lingua::EN::Inflect 1.893 and partially 1.895)
		/// </summary>
		private static class PluralRules
		{
			public static string Plural(string text, bool classicalEnglish)
			{
				Match m = __spaceRex.Match(text);
				return m.Groups[1].Value + PluralLc2(m.Groups[2].Value, true, classicalEnglish) + m.Groups[3].Value;
			}

			public static string Singular(string text, bool classicalEnglish)
			{
				Match m = __spaceRex.Match(text);
				return m.Groups[1].Value + PluralLc2(m.Groups[2].Value, false, classicalEnglish) + m.Groups[3].Value;
			}
			private static readonly Regex __spaceRex = new Regex(@"\A(\s*)(.*?)(\s*)\z");

			private static string PluralLc2(string word, bool toPlural, bool classicalEnglish)
			{
				Match m = _dualCompound.Match(word);
				if (m.Success)
					return PluralLc3(m.Groups[1].Value, toPlural, classicalEnglish) + m.Groups[2].Value +
						PluralLc3(m.Groups[3].Value, toPlural, classicalEnglish);

				m = _compound.Match(word);
				return m.Success ?
					PluralLc3(m.Groups[1].Value, toPlural, classicalEnglish) + m.Groups[2].Value:
					PluralLc3(word, toPlural, classicalEnglish);
			}

			private static string PluralLc3(string word, bool toPlural, bool classicalEnglish)
			{
				bool firstUpper = false;
				if (toPlural)
				{
					Match m = _article.Match(word);
					if (m.Success)
					{
						firstUpper = m.Groups[1].Value == "A";
						word = m.Groups[2].Value;
					}
				}
				string result;
				if (_spec.IsMatch(word))
				{
					word = PluralLc4(word[word.Length - 1] == '\'' ? word.Substring(0, word.Length - 1): word.Substring(0, word.Length - 2), toPlural, classicalEnglish);
					result = word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? word + "'": word + "'s";
				}
				else
					result = PluralLc4(word, toPlural, classicalEnglish);
				return firstUpper && result.Length > 0 ? result.Substring(0, 1).ToUpperInvariant() + result.Substring(1): result;
			}

			private static string PluralLc4(string word, bool toPlural, bool classicalEnglish)
			{
				if (word.Length == 0)
					return word;

				string? w;
				if (classicalEnglish)
				{
					w = PluralLc5(word, toPlural ? _rule.SPC: _rule.PSC);
					if (w != null)
						return SetCase(w, word);
				}
				w = PluralLc5(word, toPlural ? _rule.SPU: _rule.PSU);
				if (w == null)
					if (toPlural)
						return word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? word: word + (word.Length > 1 && Char.IsUpper(word[1]) ? "S": "s");
					else
						return word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? word.Substring(0, word.Length - 1): word;

				return SetCase(w, word);
			}

			private static string? PluralLc5(string word, OneWay r)
			{
				if (r.Map.TryGetValue(word, out string? result))
					return result;

				Match m = r.Rex!.Match(word);
				if (!m.Success)
					return null;

				for (int i = 1; i < m.Groups.Count; ++i)
				{
					if (m.Groups[i].Success)
					{
						Ending ending = r.Ending[i - 1];
						return word.Substring(0, word.Length - ending.Length) + ending.End;
					}
				}
				return null;
			}

			private const string CompoundPart = "about|above|across|after|among|around|at|athwart|before|behind|below|beneath|beside|besides|between|betwixt|beyond|but|by|during|except|for|from|in|into|near|of|off|on|onto|out|over|since|till|to|under|until|unto|upon|with";
			private static readonly Regex _compound = new Regex(@"\A\s*(.*?)((\s*-\s*|\s+)(" + CompoundPart + @"|d[eu])((\s*-\s*|\s+)(.*?))?)\s*\z", RegexOptions.IgnoreCase);
			private static readonly Regex _dualCompound = new Regex(@"\A\s*(.*?)((?:\s*-\s*|\s+)(?:" + CompoundPart + @"|d[eu])(?:\s*-\s*|\s+))a(?:\s*-\s*|\s+)(.*?)\s*\z", RegexOptions.IgnoreCase);
			private static readonly Regex _article = new Regex(@"\A\s*(a)(?:\s*-\s*|\s+)(.*?)\s*\z", RegexOptions.IgnoreCase);
			private static readonly Regex _spec = new Regex("'s?$", RegexOptions.IgnoreCase);

			#region Rules
			private static readonly Rule _rule = PrepareRule();

			private static Dictionary<string, string> GetPlurals()
			{
				return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{"a", "some"},
					{"an", "some"},
					{"this", "these"},
					{"that", "those"},

					{"my", "our"},
					{"your", "your"},
					{"its", "their"},
					{"her", "their"},
					{"his", "their"},
					{"their", "their"},

					{"am", "are"},
					{"was", "were"},
					{"have", "have"},

					{"is", "are"},

					{"are", "are"},
					{"were", "were"},

					{"i", "we"},
					{"you", "you"},
					{"she", "they"},
					{"he", "they"},
					{"it", "they"},
					{"they", "they"},

					{"me", "us"},
					{"him", "them"},
					{"them", "them"},

					{"myself", "ourselves"},
					{"yourself", "yourselves"},
					{"herself", "themselves"},
					{"himself", "themselves"},
					{"itself", "themselves"},
					{"themself", "themselves"},
				};
			}

			private static Dictionary<string, string> GetSingulars()
			{
				return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{"some", "a"},
					{"these", "this"},
					{"those", "that"},

					{"our", "my"},
					{"your", "your"},
					{"their", "its"},

					{"were", "was"},
					{"have", "have"},

					{"are", "is"},

					{"we", "i"},
					{"you", "you"},
					{"they", "it"},

					{"us", "me"},
					{"them", "it"},

					{"ourselves", "myself"},
					{"themselves", "itself"},
				};
			}

			private static Dictionary<string, string> GetIrregularsUniversal()
			{
				return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{ 
					{"corpus",		"corpuses"},
					{"opus",		"opuses"},
					{"genus",		"genera"},
					{"mythos",		"mythoi"},
					{"penis",		"penises"},
					{"testis",		"testes"},
					{"atlas",		"atlases"},
					{"yes",			"yeses"},

					{"child",		"children"},
					{"brother",		"brothers"},
					{"loaf",		"loaves"},
					{"hoof",		"hoofs"},
					{"beef",		"beefs"},
					{"thief",		"thiefs"},
					{"money",		"monies"},
					{"mongoose",	"mongooses"},
					{"ox",			"oxen"},
					{"cow",			"cows"},
					{"soliloquy",	"soliloquies"},
					{"graffito",	"graffiti"},
					{"prima donna",	"prima donnas"},
					{"octopus",		"octopuses"},
					{"genie",		"genies"},
					{"ganglion",	"ganglions"},
					{"trilby",		"trilbys"},
					{"turf",		"turfs"},
					{"numen",		"numina"},
					{"atman",		"atmas"},
					{"occiput",		"occiputs"},
					{"sabretooth",	"sabretooths"},
					{"sabertooth",	"sabertooths"},
					{"lowlife",		"lowlifes"},
					{"flatfoot",	"flatfoots"},
					{"tenderfoot",	"tenderfoots"},
					//{"Romany",		"Romanies"},
					{"romany",		"romanies"},
					//{"Tornese",		"Tornesi"},
					{"tornese",		"tornesi"},
					//{"Jerry",		"Jerrys"},
					{"jerry",		"jerries"},
					//{"Mary",		"Marys"},
					{"mary",		"maries"},
					{"talouse",		"talouses"},
					{"blouse",		"blouses"},
					//{"Rom",			"Roma"},
					{"rom",			"roma"},
					{"carmen",		"carmina"},
					{"cheval",		"chevaux"},
					{"chervonetz",	"chervontzi"},
					{"kuvasz",		"kuvaszok"},
					{"felo",		"felones"},
					{"put-off",		"put-offs"},
					{"set-off",		"set-offs"},
					{"set-out",		"set-outs"},
					{"set-to",		"set-tos"},
					{"brother-german", "brothers-german|brethren-german"},
					{"studium generale", "studia generali"},

				};
			}

			private static Dictionary<string, string> GetIrregularsClassic()
			{
				return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{ 
					{"corpus",		"corpora"},
					{"opus",		"opera"},
					{"penis",		"penes"},
					{"atlas",		"atlantes"},

					{"brother",		"brethren"},
					{"hoof",		"hooves"},
					{"beef",		"beeves"},
					{"thief",		"thieves"},
					{"cow",			"kine"},
					{"prima donna",	"prime donne"},
					{"octopus",		"octopodes"},
					{"genie",		"genii"},
					{"ganglion",	"ganglia"},
					{"turf",		"turves"},
					{"occiput",		"occipita"},
					{"oxymoron",	"oxymorona"},
				};
			}

			private static PluralRule[] GetInitialRules()
			{
				return new[]
				{
					new PluralRule(false, "", "",
						"about", "above", "across", "after", "against", "amid", "amidst", "among", "around", "as", "at", "athwart", "atop",
						"barring", "before", "behind", "below", "beneath", "beside", "besides", "between", "betwixt", "beyond", "but", "by",
						"circa", "despite", "down", "during",
						"except", "failing", "for", "from",
						"given", "in", "inside", "into",
						"like", "minus", "near", "next",
						"of", "off", "on", "onto", "out", "outside", "over",
						"pace", "past", "per", "plus", "pro",
						"qua", "round", "sans", "save", "since",
						"than", "through", "throughout", "thru", "thruout", "till", "times", "to", "toward", "towards",
						"under", "underneath", "unlike", "until", "unto", "up", "upon",
						"versus", "via", "vs", "with", "within", "without", "worth",

						"did", "had", "ate", "made", "put",
						"spent", "fought", "sank", "gave", "sought",
						"shall", "could", "ought", "should",

						"breeches", "britches", "clippers", "gallows", "hijinks",
						"headquarters", "pliers", "scissors", "testes", "herpes",
						"pincers", "shears", "proceedings", "trousers",

						"cantus", "coitus", "nexus",

						"contretemps", "corps", "debris",
						".*ois", "siemens",

						".*measles", "mumps",

						"diabetes", "jackanapes", ".*series", "species", "rabies",
						"chassis", "innings", "news", "mews", "haggis",

						".*fish", "tuna", "salmon", "mackerel", "trout",
						"bream", "sea[- ]bass", "carp", "cod", "flounder", "whiting", 

						".*deer", ".*sheep", "moose",

						"portuguese", "amoyese", "borghese", "congoese", "faroese",
						"foochowese", "genevese", "genoese", "gilbertese", "hottentotese",
						"kiplingese", "kongoese", "lucchese", "maltese", "nankingese",
						"niasese", "pekingese", "piedmontese", "pistoiese", "sarawakese",
						"shavese", "vermontese", "wenchowese", "yengeese",
						".*[nrlm]ese",

						"Portuguese", "Amoyese", "Borghese", "Congoese", "Faroese",
						"Foochowese", "Genevese", "Genoese", "Gilbertese", "Hottentotese",
						"Kiplingese", "Kongoese", "Lucchese", "Maltese", "Nankingese",
						"Niasese", "Pekingese", "Piedmontese", "Pistoiese", "Sarawakese",
						"Shavese", "Vermontese", "Wenchowese", "Yengeese",
						".*[nrlm]ese",

						".*pox",

						".*ceps",

						"graffiti", "djinn", "samuri",
						".*craft", "offspring", "pence", "quid", "hertz"),

					new PluralRule(true, "", "",
						"wildebeest", "swine", "eland", "bison", "buffalo",
						"elk", "moose", "rhinoceros", "zucchini",
						"caribou", "dace", "grouse", "guinea[- ]fowl",
						"haddock", "hake", "halibut", "herring", "mackerel",
						"pickerel", "pike", "roe", "seed", "shad",
						"snipe", "teal", "turbot", "water[- ]fowl",
					// us -> us
						"status", "apparatus", "prospectus", "sinus",
						"hiatus", "impetus", "plexus"),

					new PluralRule(true, "is", "ides",
						"ephemeris", "iris", "clitoris", "chrysalis", "epididymis",
						".*itis"),

					new PluralRule(true, "a", "ata",
						"anathema", "bema", "carcinoma", "charisma", "diploma",
						"dogma", "drama", "edema", "enema", "enigma", "lemma",
						"lymphoma", "magma", "melisma", "miasma", "oedema",
						"sarcoma", "schema", "soma", "stigma", "stoma", "trauma",
						"gumma", "pragma"),

					new PluralRule(false, "a", "ae",
						"alumna", "alga", "vertebra", "persona"),
					new PluralRule(true, "a", "ae",
						"amoeba", "antenna", "formula", "hyperbola",
						"medusa", "nebula", "parabola", "abscissa",
						"hydra", "nova", "lacuna", "aurora", "flora", "fauna",
						".*umbra"),

					new PluralRule(true, "men", "mina",
						"stamen", "foramen", "lumen"),

					new PluralRule(false, "um", "a",
						"bacterium", "agendum", "desideratum", "erratum",
						"stratum", "datum", "ovum", "extremum", "candelabrum",
						"intermedium", "malum", "Progymnasium"),
					new PluralRule(true, "um", "a",
						"maximum", "minimum", "momentum", "optimum",
						"quantum", "cranium", "curriculum", "dictum",
						"phylum", "aquarium", "compendium", "emporium",
						"enconium","gymnasium", "honorarium", "interregnum",
						"lustrum", "memorandum", "millennium", "rostrum", 
						"spectrum","speculum","stadium", "trapezium",
						"ultimatum","medium","vacuum","velum", 
						"consortium"),

					new PluralRule(false, "us", "i",
						"alumnus", "alveolus", "bacillus", "bronchus",
						"locus", "nucleus", "stimulus", "meniscus",
						"sarcophagus", "interradius", "perradius", "triradius"),
					new PluralRule(true, "us", "i",
						"focus", "radius", "genius",
						"incubus", "succubus", "nimbus",
						"fungus", "nucleolus", "stylus",
						"torus", "umbilicus", "uterus",
						"hippopotamus", "cactus"),

					new PluralRule(false, "on", "a",
						"criterion", "perihelion", "aphelion",
						"phenomenon", "prolegomenon", "noumenon",
						"organon", "asyndeton", "hyperbaton", "legomenon"),
					new PluralRule(true, "on", "a",
						"oxymoron"),

					new PluralRule(false, "o", "os",
						"^ado",			"aficionado",	"aggro",
						"albino",		"allegro",		"ammo",
						"Antananarivo",	"archipelago",	"armadillo",
						"auto",			"avocado",		"Bamako",
						"Barquisimeto",	"bimbo",		"bingo",
						"Biro",			"bolero",		"Bolzano",
						"bongo",		"Boto",			"burro",
						"Cairo",		"canto",		"cappuccino",
						"casino",		"cello",		"Chicago",
						"Chimango",		"cilantro",		"cochito",
						"coco",			"Colombo",		"Colorado",     
						"commando",		"concertino",	"contango",
						"credo",		"crescendo",	"cyano",
						"demo",			"ditto",		"Draco",
						"dynamo",		"embryo",		"Esperanto",
						"espresso",		"euro",			"falsetto",
						"Faro",			"fiasco",		"Filipino",
						"flamenco",		"furioso",		"generalissimo",
						"Gestapo",		"ghetto",		"gigolo",
						"gizmo",		"Greensboro",	"gringo",
						"Guaiabero",	"guano",		"gumbo",
						"gyro",			"hairdo",		"hippo",
						"Idaho",		"impetigo",		"inferno",
						"info",			"intermezzo",	"intertrigo",
						"Iquico",		"^ISO",			 "jumbo",
						"junto",		"Kakapo",		"kilo",
						"Kinkimavo",	"Kokako",		"Kosovo",
						"Lesotho",		"libero",		"libido",
						"libretto",		"lido",			"Lilo", 
						"limbo",		"limo",			"lineno",
						"lingo",		"lino",			"livedo",
						"loco",			"logo",			"lumbago",
						"macho",		"macro",		"mafioso",
						"magneto",		"magnifico",	"Majuro",
						"Malabo",		"manifesto",	"Maputo",
						"Maracaibo",	"medico",		"memo",
						"metro",		"Mexico",		"micro",
						"Milano",		"Monaco",		"mono", 
						"Montenegro",	"Morocco",		"Muqdisho",
						"myo",			"^NATO",		 "^NCO",
						"neutrino",		"^NGO",			 "Ningbo",
						"octavo",		"oregano",		"Orinoco",
						"Orlando",		"Oslo",			"^oto",
						"panto",		"Paramaribo",	"Pardusco",
						"pedalo",		"photo",		"pimento",
						"pinto",		"pleco",		"Pluto",
						"pogo",			"polo",			"poncho",
						"Porto-Novo",	"Porto",		"pro",
						"psycho",		"pueblo",		"quarto",
						"Quito",		"rhino",		"risotto",
						"rococo",		"rondo",		"Sacramento",
						"saddo",		"sago",			"salvo",
						"Santiago",		"Sapporo",		"Sarajevo",
						"scherzando",	"scherzo",		"silo",
						"sirocco",		"sombrero",		"staccato",
						"sterno",		"stucco",		"stylo",
						"sumo",			"Taiko",		"techno",
						"terrazzo",		"testudo",		"timpano",
						"tiro",			"tobacco",		"Togo",
						"Tokyo",		"torero",		"Torino",
						"Toronto",		"torso",		"tremolo",
						"typo",			"tyro",			"ufo",
						"UNESCO",		"vaquero",		"vermicello",
						"verso",		"vibrato",		"violoncello",
						"Virgo",		"weirdo",		"WHO",  
						"WTO",			"Yamoussoukro",	"yo-yo",        
						"zero",			"Zibo",
						".*[aeiou]o",
						// from Classical o->i
						"solo",  "soprano", "basso", "alto",
						"contralto", "tempo", "piano", "virtuoso"),

					new PluralRule(true, "o", "i",
						"solo",  "soprano", "basso", "alto",
						"contralto", "tempo", "piano", "virtuoso"),

					new PluralRule(false, "ex", "ices",
						"codex", "murex", "silex"),
					new PluralRule(true, "ex", "ices",
						"vortex", "vertex", "cortex", "latex",
						"pontifex", "apex",  "index", "simplex"),

					new PluralRule(false, "ix", "ices",
						"radix", "helix"),
					new PluralRule(true, "ix", "ices",
						"appendix"),

					new PluralRule(true, "", "i",
						"afrit", "afreet", "efreet"),

					new PluralRule(true, "", "im",
						"goy",  "seraph", "cherub", "zuz", "kibbutz"),

					new PluralRule(false, "man", "mans",
						"ataman", "caiman", "cayman", "ceriman",
						"desman", "dolman", "farman", "harman", "hetman",
						"human", "leman", "ottoman", "shaman", "talisman",
						"alabaman", "bahaman", "burman", "german",
						"hiroshiman", "liman", "nakayaman", "oklahoman",
						"panaman", "selman", "sonaman", "tacoman", "yakiman",
						"yokohaman", "yuman"),

					//// 3rd person
					//new Rule(false, "es", "",
					//    ".*[cs]hes",
					//    ".*xes",
					//    ".*zzes",
					//    ".*sses"),
					//new Rule(false, "ies", "y",
					//    "..+ies"),
					//new Rule(false, "oes", "o",
					//    "..+oes"),

					new PluralRule(false, "man", "men",
						".*man"),
					new PluralRule(false, "ouse", "ice",
						".*[ml]ouse"),
					new PluralRule(false, "goose", "geese",
						".*gouse"),
					new PluralRule(false, "tooth", "teeth",
						".*tooth"),
					new PluralRule(false, "foot", "feet",
						".*foot"),
					new PluralRule(false, "zoon", "zoa",
						".*zoon"),

					new PluralRule(true, "trix", "trices",
						".*trix"),
					new PluralRule(true, "eau", "eaux",
						".*eau"),
					new PluralRule(true, "ieu", "ieux",
						".*ieu"),
					new PluralRule(true, "nx", "nges",
						"..+[yia]nx"),

					new PluralRule(false, "", "es",
						".*ss",
						"acropolis", "aegis", "alias", "asbestos", "bathos", "bias",
						"bronchitis", "bursitis", "caddis", "cannabis",
						"canvas", "chaos", "cosmos", "dais", "digitalis",
						"epidermis", "ethos", "eyas", "gas", "glottis", 
						"hubris", "ibis", "lens", "mantis", "marquis", "metropolis",
						"pathos", "pelvis", "polis", "rhinoceros",
						"sassafras", "trellis",
						
						//"[A-Z].*s",
						".*[cs]h",
						".*x",
						".*zz",
						".*ss",
						".*us",
						".*o",

						"ephemeris", "iris", "clitoris", "chrysalis", "epididymis",
						".*itis"),

					new PluralRule(false, "is", "es",
						".*[csx]is"),

					new PluralRule(false, "f", "ves",
						".*[eao]lf",
						".*[^d]eaf",
						".*[nlw]ife",
						".*arf"),
					new PluralRule(false, "ife", "ives",
						".*[nlw]ife"),

					new PluralRule(false, "y", "ys",
						".*[aeiou]y"),
					new PluralRule(false, "y", "ies",
						".*y"),
					new PluralRule(false, "z", "zzes",
						".*[^z]z"),
				};
			}

			private struct PluralRule
			{
				public readonly bool Classical;
				public readonly string SingualEnding;
				public readonly string PluralEnding;
				public readonly string Words;

				public PluralRule(bool classical, string singularEnding, string pluralEnding, params string[] words)
				{
					Classical = classical;
					SingualEnding = singularEnding;
					PluralEnding = pluralEnding;
					Words = String.Join("|", words);
				}

				public PluralRule(bool classical, string singularEnding, string pluralEnding, string words)
				{
					Classical = classical;
					SingualEnding = singularEnding;
					PluralEnding = pluralEnding;
					Words = words;
				}

				//public void Reverse()
				//{
				//    if (SingualEnding.Length > 0)
				//        Words = Regex.Replace(Words, SingualEnding + "(?=\\|)|" + SingualEnding + "$", PluralEnding);
				//    else if (PluralEnding.Length > 0)
				//        Words = Words.Replace("|", PluralEnding + "|") + PluralEnding;
				//}
			}

			readonly struct Ending
			{
				public readonly int Length;
				public readonly string End;

				public Ending(int length, string end)
				{
					Length = length;
					End = end;
				}
			}

			class OneWay
			{
				public Dictionary<string, string> Map;
				public Regex? Rex;
				public List<Ending> Ending;

				public OneWay(Dictionary<string, string> map)
				{
					Map = map;
					Ending = new List<Ending>();
				}
			}

			class Rule
			{
				public readonly OneWay SPU;
				public readonly OneWay SPC;
				public readonly OneWay PSU;
				public readonly OneWay PSC;

				public Rule(Dictionary<string, string> pluralUniversal, Dictionary<string, string> pluralClassic, Dictionary<string, string> adjPlural, Dictionary<string, string> adjSingular)
				{
					PSU = new OneWay(Join(Reverse(pluralUniversal), adjSingular));
					PSC = new OneWay(Reverse(pluralClassic));
					SPU = new OneWay(Join(pluralUniversal, adjPlural));
					SPC = new OneWay(pluralClassic);
				}
			}

			private static Dictionary<string, string> Join(Dictionary<string, string> left, Dictionary<string, string> right)
			{
				foreach (var item in right)
				{
					left.Add(item.Key, item.Value);
				}
				return left;
			}

			private static Dictionary<string, string> Reverse(Dictionary<string, string> map)
			{
				var result = new Dictionary<string, string>(map.Count, map.Comparer);
				foreach (var item in map)
				{
					result[item.Value] = item.Key;
				}
				return result;
			}

			private static Rule PrepareRule()
			{
				PluralRule[] rr = GetInitialRules();
				var ir = new Rule(GetIrregularsUniversal(), GetIrregularsClassic(), GetPlurals(), GetSingulars());

				var rspu = new StringBuilder("\\A(?:");
				var rspc = new StringBuilder("\\A(?:");
				var rpsu = new StringBuilder("\\A(?:");
				var rpsc = new StringBuilder("\\A(?:");

				for (int i = 0; i < rr.Length; ++i)
				{
					int k = 0;
					string?[] ww = rr[i].Words.Split('|');
					for (int j = 0; j < ww.Length; ++j)
					{
						string s = ww[j]!;
						string w = s.Substring(0, s.Length - rr[i].SingualEnding.Length);
						ww[j] = w;
						if (w.IndexOfAny(new[] { '.', '[' }) < 0)
						{
							string p = w + rr[i].PluralEnding;
							if (rr[i].Classical)
							{
								ir.SPC.Map[s] = p;
								ir.PSC.Map[p] = s;
							}
							else
							{
								ir.SPU.Map[s] = p;
								ir.PSU.Map[p] = s;
							}
							ww[j] = null;
							++k;
						}
					}
					if (k < ww.Length)
					{
						if (rr[i].Classical)
						{
							ir.SPC.Ending.Add(new Ending(rr[i].SingualEnding.Length, rr[i].PluralEnding));
							Append(rspc, ww, rr[i].SingualEnding);
							ir.PSC.Ending.Add(new Ending(rr[i].PluralEnding.Length, rr[i].SingualEnding));
							Append(rpsc, ww, rr[i].PluralEnding);
						}
						else
						{
							ir.SPU.Ending.Add(new Ending(rr[i].SingualEnding.Length, rr[i].PluralEnding));
							Append(rspu, ww, rr[i].SingualEnding);
							if (rr[i].SingualEnding != "is")
							{
								ir.PSU.Ending.Add(new Ending(rr[i].PluralEnding.Length, rr[i].SingualEnding));
								Append(rpsu, ww, rr[i].PluralEnding);
							}
						}
					}
				}
				--rspu.Length;
				--rspc.Length;
				--rpsu.Length;
				--rpsc.Length;
				rspu.Append(")\\z");
				rpsu.Append(")\\z");
				rspc.Append(")\\z");
				rpsc.Append(")\\z");
				ir.SPU.Rex = new Regex(rspu.ToString(), RegexOptions.IgnoreCase);
				ir.PSU.Rex = new Regex(rpsu.ToString(), RegexOptions.IgnoreCase);
				ir.SPC.Rex = new Regex(rspc.ToString(), RegexOptions.IgnoreCase);
				ir.PSC.Rex = new Regex(rpsc.ToString(), RegexOptions.IgnoreCase);

				return ir;
			}

			private static void Append(StringBuilder text, string?[] values, string ending)
			{
				text.Append('(');
				bool next = false;
				for (int i = 0; i < values.Length; ++i)
				{
					if (values[i] != null)
					{
						if (next)
							text.Append('|');
						else
							next = true;
						text.Append(values[i]).Append(ending);
					}
				}
				text.Append(")|");
			}
			#endregion
		}

		private static class NumberRules
		{
			public static string Ord(long value)
			{
				return value.ToString(CultureInfo.InvariantCulture) + GetOrdinalPostfix(value);
			}

			public static string Ord(decimal value)
			{
				return value.ToString($"0'{GetOrdinalPostfix((long)(value % 100))}'.###############", CultureInfo.InvariantCulture);
			}

			public static string Ord(string value)
			{
				if (value.Length == 0)
					return value;

				if (Int64.TryParse(value, NumberStyles.Any, null, out long n))
				{
					if (Char.IsDigit(value, value.Length - 1))
						return value + GetOrdinalPostfix(n);

					for (int i = value.Length - 2; i >= 0; --i)
					{
						if (Char.IsDigit(value, i))
							return value.Substring(0, i + 1) + GetOrdinalPostfix(n) +
								(Char.IsLetter(value, i + 1) ? " " + value.Substring(i + 1): value.Substring(i + 1));
					}
					return value;
				}
				return __nthRex.Replace(value, m => SetCase(m.Value.Length == 1 ? m.Value + "th": __nth[m.Value], m.Value), 1);
			}

			public static string NumWord(string value, string? comma, string? and)
			{
				comma = Separator(comma, ", ");
				and = Separator(and, " and ");

				value = value.TrimStart();
				if (value.Length == 0)
					return __unit[0];
				bool minus = false;
				if (value[0] == '-')
				{
					minus = true;
					value = value.Substring(1).Trim();
				}
				if (value.Length == 0)
					return __unit[0];

				value = __nonDigitsRex.Replace(value, "");
				value = __leadingZeroesRex.Replace(value, "");
				string fract = "";
				int i = value.IndexOf('.');
				if (i >= 0)
				{
					fract = __pointsRex.Replace(value.Substring(i), "");
					value = value.Substring(0, i);
				}

				i = -1;
				value = __threeDigitsRex.Replace(value, m =>
				{
					++i;
					string ss = m.Value.Substring(1);
					return ss == "00" ?
						m.Value == "000" ? "": __unit[m.Value[0] - '0'] + " hundred" + Mill(i) + comma:
						m.Value[0] == '0' ? Ten(ss, i) + comma:
							__unit[m.Value[0] - '0'] + " hundred" + and + Ten(ss, i) + comma;
				});

				value = __twoDigitsRex.Replace(value, m => Ten(m.Value, ++i) + comma);
				value = __oneDigitRex.Replace(value, m => __unit[m.Value[0] - '0'] + Mill(++i) + comma);
				if (value.Length >= comma.Length)
					value = value.Substring(0, value.Length - comma.Length);
				if (fract.Length > 0)
					fract = " point" + Regex.Replace(fract, @"\d", m => " " + __unit[m.Value[0] - '0']);
				return minus ? "minus " + value + fract: value + fract;
			}
			private static readonly Regex __nonDigitsRex = new Regex(@"[^0-9\.]+");
			private static readonly Regex __leadingZeroesRex = new Regex(@"\A0+");
			private static readonly Regex __pointsRex = new Regex(@"\.+");
			private static readonly Regex __threeDigitsRex = new Regex(@"\d\d\d", RegexOptions.RightToLeft);
			private static readonly Regex __twoDigitsRex = new Regex(@"\d\d", RegexOptions.RightToLeft);
			private static readonly Regex __oneDigitRex = new Regex(@"\d");


			public static string NumWord(decimal value, string? comma, string? and)
			{
				comma = Separator(comma, ", ");
				and = " hundred" + Separator(and, " and ");

				var text = new StringBuilder();
				if (value < 0)
				{
					value = -value;
					text.Append("minus ");
				}
				decimal integral = Decimal.Truncate(value);
				decimal fract = value - integral;
				string[] parts = new string[12];
				int i = 0;

				while (integral >= 1)
				{
					int d = (int)(integral % 1000);
					integral /= 1000;
					if (d > 0)
					{
						int dd = d % 100;
						d /= 100;
						if (d == 0)
							parts[i] = Ten(dd);
						else if (dd == 0)
							parts[i] = __unit[d] + " hundred";
						else
							parts[i] = __unit[d] + and + Ten(dd);
					}
					++i;
				}
				while (i > 0)
				{
					--i;
					if (parts[i] != null)
					{
						text.Append(parts[i]).Append(__mill[i]);
						if (i > 0)
							text.Append(comma);
					}
				}

				if (fract > 0)
				{
					if (integral > 0)
						text.Append(' ');
					text.Append("point");
					while (fract > 0)
					{
						fract *= 10;
						integral = Decimal.Truncate(fract);
						fract -= integral;
						text.Append(' ');
						text.Append(__unit[(int)integral]);
					}
				}
				return text.ToString();
			}

			public static string GetOrdinalPostfix(long value)
			{
				int n = (int)(value % 100);
				if (n < 0)
					n = -n;
				if (n > 19)
					n %= 10;
				return n == 1 ? "st" : (n == 2 ? "nd" : (n == 3 ? "rd" : "th"));
			}

			private static string Separator(string? value, string defaultValue)
			{
				if (value == null)
					return defaultValue;

				value = value.TrimEnd() + " ";
				return Char.IsLetter(value[0]) ? " " + value: value;
			}

			private static string Mill(int index)
			{
				return index >= __mill.Length ? __mill[__mill.Length - 1]: __mill[index];
			}

			private static string Ten(string value, int mill)
			{
				Debug.Assert(value.Length == 2);

				if (value[0] == '0')
					return __unit[value[1] - '0'] + Mill(mill);
				if (value[0] == '1')
					return __teen[value[1] - '0'] + Mill(mill);
				if (value[1] == '0')
					return __ten[value[0] - '0'] + Mill(mill);
				return __ten[value[0] - '0'] + "-" + __unit[value[1] - '0'] + Mill(mill);
			}

			private static string Ten(int value)
			{
				if (value < 10)
					return __unit[value];
				if (value < 20)
					return __teen[value - 10];
				int d0 = value % 10;
				int d1 = value / 10;
				if (d0 == 0)
					return __ten[d1];
				return __ten[d1] + "-" + __unit[d0];
			}

			private static readonly Dictionary<string, string> __nth = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{"ty", "tieth"},
				{"one", "first"},
				{"two", "second"},
				{"three", "third"},
				{"five", "fifth"},
				{"eight", "eighth"},
				{"nine", "ninth"},
				{"twelve", "twelfth"},
			};
			private static readonly Regex __nthRex = new Regex(@"(ty|one|two|three|five|eight|nine|twelve|[a-z])(?=\s*\Z|\s+point)", RegexOptions.IgnoreCase);
			private static readonly string[] __unit = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
			private static readonly string[] __teen = { "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
			private static readonly string[] __ten = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
			private static readonly string[] __mill = { "", " thousand", " million", " billion", " trillion", " quadrillion", " quintillion", " sextillion", " septillion", " octillion", " nonillion", " decillion" };

			//private static Dictionary<string, string> __nth = BuildNth();
			//private static Regex __nthRex = new Regex("(" + String.Join("|", __nth.Keys) + ")(?=\\W|\\Z)", RegexOptions.IgnoreCase);
			//private static string[] __unitTh = new string[] { "zero", "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth" };

			//private static Dictionary<string, string> BuildNth()
			//{
			//    Dictionary<string, string> nth = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			//    for (int i = 0; i < __unit.Length; ++i)
			//    {
			//        nth.Add(__unit[i], __unitTh[i]);
			//    }
			//    for (int i = 0; i < __teen.Length; ++i)
			//    {
			//        nth.Add(__teen[i], __teen[i] + "th");
			//    }
			//    for (int i = 2; i < __ten.Length; ++i)
			//    {
			//        nth.Add(__ten[i], __ten[i] + "th");
			//    }
			//    nth["twelve"] = "twelfth";
			//    return nth;
			//}
		}
	}
}
