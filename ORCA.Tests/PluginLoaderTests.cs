using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ArduinoAPI;
using Microsoft.CodeAnalysis.CSharp;
using ORCA.Core;
using ORCA.Plugin;
using Xunit;

namespace ORCA.Tests
{
    public class PluginLoaderTests
    {
        const string BOILERPLATE =
            //　lang=c#
            """
            global using System.Threading;
            global using ArduinoAPI;
            global using ORCA.Plugin;

            public class Cmd : MacroCommand
            {
                public override void Execute(IWritable port, in CancellationToken token, IMacroContext context) { }
            }
            """;

        static readonly MetadataReference[] References =
        [
            .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Append(typeof(MacroCommand).Assembly.Location)
                .Append(typeof(IWritable).Assembly.Location)
                .Distinct()
                .Select(_ => MetadataReference.CreateFromFile(_))
        ];

        static string CompilePlugin(string source)
        {
            var name = "Plugin_" + Guid.NewGuid().ToString("N");
            var compilation = CSharpCompilation.Create(
                name,
                [CSharpSyntaxTree.ParseText(BOILERPLATE), CSharpSyntaxTree.ParseText(source)],
                References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var dir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "TestPlugins", name)).FullName;
            using (var stream = File.Create(Path.Combine(dir, name + ".dll")))
            {
                var result = compilation.Emit(stream);
                if (!result.Success)
                    throw new Exception(string.Join(Environment.NewLine, result.Diagnostics));
            }

            return dir;
        }

        [Fact]
        public void MacroCommand属性がついていてIMacroCommandParserを実装している具象型が抽出されること()
        {
            // Arrange
            const string plugin = 
                // lang=c#
                """
                [MacroCommand("Reset")]
                public class ResetCommandParser : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;

            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin)).ToArray();

            // Assert
            Assert.Equal("Reset", Assert.Single(customCommands).CommandName);
        }

        [Fact]
        public void 複数のコマンドが定義されていれば全て抽出されること()
        {
            // Arrange
            const string plugin =
                // lang=c#
                """
                [MacroCommand("Reset")]
                public class ResetCommandParser : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }

                [MacroCommand("Skip")]
                public class SkipCommandParser : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;

            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin)).ToArray();

            // Assert
            Assert.Equal(["Reset", "Skip"], customCommands.Select(_ => _.CommandName).Order());
        }

        [Fact]
        public void Genericなクラスは抽出対象にならないこと()
        {
            // Arrange
            const string plugin =
                // lang=c#
                """
                [MacroCommand("Bad1")]
                public class GenericParser<T> : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;
            
            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin));

            // Assert
            Assert.Empty(customCommands);
        }

        [Fact]
        public void 抽象クラスは抽出対象にならないこと()
        {
            // Arrange
            const string plugin =
                // lang=c#
                """
                [MacroCommand("Bad2")]
                public abstract class AbstractParser : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;

            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin));

            // Assert
            Assert.Empty(customCommands);
        }

        [Fact]
        public void 引数なしのコンストラクタが定義されていないクラスは抽出対象にならないこと()
        {
            // Arrange
            const string plugin =
                // lang=c#
                """
                [MacroCommand("Bad4")]
                public class NeedsArgumentParser : IMacroCommandParser<Cmd>
                {
                    public NeedsArgumentParser(int _) { }

                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;
            
            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin));

            // Assert
            Assert.Empty(customCommands);
        }

        [Fact]
        public void MacroCommandAttribute属性が付いていないクラスは抽出対象にならないこと()
        {
            // Arrange
            const string plugin =
                // lang=c#
                """
                public class NoAttributeParser : IMacroCommandParser<Cmd>
                {
                    public Cmd Parse(string[] args, IMacroParserContext context, out string errorMessage) { errorMessage = ""; return new Cmd(); }
                }
                """;
            
            // Act
            var customCommands = PluginLoader.Load(CompilePlugin(plugin));

            // Assert
            Assert.Empty(customCommands);
        }

        [Fact]
        public void Pluginフォルダが無ければ空配列を返すこと()
        {
            // Arrange
            var missing = Path.Combine(AppContext.BaseDirectory, "存在しないフォルダ");

            // Act
            var customCommands = PluginLoader.Load(missing);

            // Assert
            Assert.Empty(customCommands);
        }
    }
}
