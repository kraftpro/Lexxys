namespace Lexxys.Argument.Tests;

public class Parameters
{
    public static ArgumentsBuilder LsParameters() => new ArgumentsBuilder()
        .Switch("all", "a", "do not ignore entries starting with .")
        .Switch("almost-all", new[] { "A" }, "do not list implied . and ..")
        .Switch("author", description: "with -l, print the author of each file")
        .Switch("escape", "b", description: "print C-style escapes for nongraphic characters")
        .Parameter("block-size", valueName: "SIZE", description: "with -l, scale sizes by SIZE when printing them; e.g., '--block-size=M'; see SIZE format below")
        .Switch("ignore-backups", "B", description: "do not list implied entries ending with ~")
        .Switch("c", description: "with -lt: sort by, and show, ctime (time of last modification of file status information); with -l: show ctime and sort by name; otherwise: sort by ctime, newest first")
        .Switch("C", description: "list entries by columns")
        .Parameter("color", valueName: "WHEN", description: "color the output WHEN; more info below")
        .Switch("directory", "d", description: "list directories themselves, not their contents")
        .Switch("dired", "D", description: "generate output designed for Emacs' dired mode")
        .Switch("f", description: "list all entries in directory order")
        .Parameter("classify", "F", valueName: "WHEN", description: "append indicator (one of */=>@|) to entries WHEN")
        .Switch("file-type", description: "likewise, except do not append '*'")
        .Parameter("format", valueName: "WORD", description: "across -x, commas -m, horizontal -x, long -l, single-column -1, verbose -l, vertical -C")
        .Switch("full-time", description: "like -l --time-style=full-iso")
        .Switch("g", description: "like -l, but do not list owner")
        .Switch("group-directories-first", description: "group directories before files; can be augmented with a --sort option, but any use of --sort=none (-U) disables grouping")
        .Switch("no-group", "G", description: "in a long listing, don't print group names")
        .Switch("human-readable", "h", description: "with -l and -s, print sizes like 1K 234M 2G etc.")
        .Switch("si", description: "likewise, but use powers of 1000 not 1024")
        .Switch("dereference-command-line", "H", description: "follow symbolic links listed on the command line")
        .Switch("dereference-command-line-symlink-to-dir", description: "follow each command line symbolic link that points to a directory")
        .Parameter("hide", valueName: "PATTERN", description: "do not list implied entries matching shell PATTERN (overridden by -a or -A)")
        .Parameter("hyperlink", valueName: "WHEN", description: "hyperlink file names WHEN")
        .Parameter("indicator-style", "p", valueName: "SLASH", description: "append indicator with style SLASH to entry names: none (default), slash (-p), file-type (--file-type), classify (-F)")
        .Switch("inode", "i", description: "print the index number of each file")
        .Parameter("ignore", "I", valueName: "PATTERN", description: "do not list implied entries matching shell PATTERN")
        .Switch("kibibytes", "k", description: "default to 1024-byte blocks for file system usage; used only with -s and per directory totals")
        .Switch("l", description: "use a long listing format")
        .Switch("dereference", "L", description: "when showing file information for a symbolic link, show information for the file the link references rather than for the link itself")
        .Switch("m", description: "fill width with a comma separated list of entries")
        .Switch("numeric-uid-gid", "n", description: "like -l, but list numeric user and group IDs")
        .Switch("literal", "N", description: "print entry names without quoting")
        .Switch("o", description: "like -l, but do not list group information")
        .Switch("hide-control-chars", "q", description: "print ? instead of nongraphic characters")
        .Switch("show-control-chars", description: "show nongraphic characters as-is (the default, unless program is 'ls' and output is a terminal)")
        .Switch("quote-name", "Q", description: "enclose entry names in double quotes")
        .Parameter("quoting-style", valueName: "WORD", description: "use quoting style WORD for entry names: literal, locale, shell, shell-always, shell-escape, shell-escape-always, c, escape (overrides QUOTING_STYLE environment variable)")
        .Switch("reverse", "r", description: "reverse order while sorting")
        .Switch("recursive", "R", description: "list subdirectories recursively")
        .Switch("size", "s", description: "print the allocated size of each file, in blocks")
        .Switch("S", description: "sort by file size, largest first")
        .Parameter("sort", valueName: "WORD", description: "sort by WORD instead of name: none (-U), size (-S), time (-t), version (-v), extension (-X), width")
        .Parameter("time", valueName: "WORD", description: "change the default of using modification times; access time (-u): atime, access, use; change time (-c): ctime, status; birth time: birth, creation;  with -l, WORD determines which time to show; with --sort=time, sort by WORD (newest first)")
        .Parameter("time-style", valueName: "TIME_STYLE", description: "time/date format with -l; see TIME_STYLE below")
        .Switch("t", description: "sort by time, newest first; see --time")
        .Parameter("tabsize", "T", valueName: "COLS", description: "assume tab stops at each COLS instead of 8")
        .Switch("u", description: "with -lt: sort by, and show, access time; with -l: show access time and sort by name; otherwise: sort by access time, newest first")
        .Switch("U", description: "do not sort; list entries in directory order")
        .Switch("v", description: "natural sort of (version) numbers within text")
        .Parameter("width", "w", valueName: "COLS", description: "set output width to COLS.  0 means no limit")
        .Switch("x", description: "list entries by lines instead of by columns")
        .Switch("X", description: "sort alphabetically by entry extension")
        .Switch("context", "Z", description: "print any security context of each file")
        .Switch("zero", description: "end each output line with NUL, not newline")
        .Switch("1", description: "list one file per line")
        .Switch("help", description: "display this help and exit")
        .Switch("version", description: "output version information and exit")
        .Positional("FILE", description: "files to list", collection: true)
        ;

    public static ArgumentsBuilder ObjParameters() => new ArgumentsBuilder()
        .Switch("version", description: "show version value")
        .Switch("test", "T", description: "test mode")
        .Switch("verbose", "V", description: "verbose output")
        .Switch("debug", "D", description: "output debut info")
        .Help()
        .Positional("@script-file", description: "script file to execute", collection: true)
        .BeginCommand("create", "creates a new object")
            .Positional("name", description: "name of the object to create", required: true)
            .Parameter("type", "T", description: "type of the object")
            .Parameter("value", description: "value of the object")
            .Switch("readonly", "R", description: "read only object")
            .BeginCommand("factory", "creates an object factory")
                .Switch("readonly", "R", description: "set the factory as a read-only")
            .EndCommand()
            .BeginCommand("collection", "creates a collection of objects")
                .Parameter("count", "C", description: "number of objects in the collection")
                .Switch("readonly", "R", description: "set the collection as a read-only")
            .EndCommand()
        .EndCommand()
        .BeginCommand("delete", "deletes an object")
            .Positional("name", description: "name of the object to delete", required: true)
            .Switch("force", "F", description: "force delete")
            .Switch("permanent", "P", description: "permanent delete")
        .EndCommand()
        .BeginCommand("list", "lists objects")
            .Parameter("filter", "F", description: "filter for the list")
            .Switch("table", "T", description: "table mode")
            .Switch("list", "L", description: "table mode")
            .Parameter("sort", "O", description: "sort objects (N - by name; T - by time of creation)")
        .EndCommand()
        .BeginCommand("update", "updates an object")
            .Positional("name", description: "name of the object to update", required: true)
            .Positional("value", description: "new value of the object", required: true)
            .Switch("force", "F", description: "force update")
        .EndCommand()
        .BeginCommand("run", "runs an factory")
            .Positional("name", description: "name of the object", required: true)
            .Parameter("count", "C", description: "number of objects to create (1)")
        .EndCommand()
        ;
}
