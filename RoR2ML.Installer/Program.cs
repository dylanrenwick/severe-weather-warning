using System;
using System.Linq;
using System.IO;
using System.Net.Mime;
using Mono.Cecil;
using Mono.Cecil.Cil;

using RoR2ML;
using RoR2ML.Logging;

namespace RoR2ML.Installer
{
    internal class Program
    {
        private const string GAME_DIR_NAME = "Risk of Rain 2";
        private const string LOADER_ASSEMBLY_NAME = "RoR2ML.dll";
        
        private const string ASSEMBLY_ENTRY_POINT_TYPE = "RoR2.UI.MainMenu.MainMenuController";
        private const string ASSEMBLY_ENTRY_POINT_METHOD = "Start";

        private const string LOADER_ENTRY_POINT_TYPE = "RoR2ML.Loader";
        private const string LOADER_ENTRY_POINT_METHOD = "Init";
        
        private static BaseAssemblyResolver asmResolver;
        
        public static void Main(string[] args)
        {
            Logger.onLog += onLoggerLog;
            Logger.showModuleNames = false;
            Logger.showTimestamps = false;
            Logger.Begin("installer.log");
            
            asmResolver = new DefaultAssemblyResolver();
            
            FileInfo assemblyFilePath = getAssemblyFilePath();
            if (assemblyFilePath == null)
            {
                error("Couldn't find assembly");
                return;
            }
            if (!canOpenFile(assemblyFilePath)) error("Could not open file " + assemblyFilePath.FullName);
            log("Found the file at " + assemblyFilePath.FullName);
            
            string loaderFilePath = getLoaderFilePath();
            if (!canOpenFile(loaderFilePath)) error("Could not open file " + loaderFilePath);

            string gamePath = assemblyFilePath.Directory.Parent.FullName;
            string fileName = Path.DirectorySeparatorChar + LOADER_ASSEMBLY_NAME;
            
            log("Finding files...");
            if (!File.Exists(gamePath + fileName))
                File.Copy(loaderFilePath, gamePath + fileName);
            loaderFilePath = gamePath + fileName;
            if (!canOpenFile(loaderFilePath)) error("Could not open file " + loaderFilePath);
            
            // Load both modules
            log("Loading modules...");
            ModuleDefinition assemblyModule = ModuleDefinition.ReadModule(assemblyFilePath.FullName, 
                new ReaderParameters { AssemblyResolver = asmResolver, ReadingMode = ReadingMode.Immediate });
            ModuleDefinition loaderModule = ModuleDefinition.ReadModule(loaderFilePath, 
                new ReaderParameters { AssemblyResolver = asmResolver, ReadingMode = ReadingMode.Immediate });
            
            log("Finding loader entry point type...");
            // Find loader entry point type
            TypeDefinition loaderEntryPointType = loaderModule.GetType(LOADER_ENTRY_POINT_TYPE);
            if (loaderEntryPointType == null) error("Could not find entry point type in loader: "
                                                    + LOADER_ENTRY_POINT_TYPE);
            
            log("Injecting type import...");
            loaderEntryPointType = assemblyModule.ImportReference(loaderEntryPointType).Resolve();
            
            log("Finding loader entry point method...");
            // Find loader entry point method
            MethodReference loaderEntryPointMethod = loaderEntryPointType.Methods.FirstOrDefault(x => 
                x.Name == LOADER_ENTRY_POINT_METHOD
            );
            if (loaderEntryPointMethod == null) error("Could not find entry point method in loader: "
                                                      + LOADER_ENTRY_POINT_TYPE + "." + LOADER_ENTRY_POINT_METHOD);
            
            log("Injecting method import...");
            loaderEntryPointMethod = assemblyModule.ImportReference(loaderEntryPointMethod);
            
            log("Finding game entry point...");
            // Find assembly entry point type
            TypeDefinition assemblyEntryPointType = assemblyModule.GetType(ASSEMBLY_ENTRY_POINT_TYPE);
            if (assemblyEntryPointType == null) error("Could not find entry point type in game: "
                                                      + ASSEMBLY_ENTRY_POINT_TYPE);
            // Find assembly entry point method
            MethodDefinition assemblyEntryPointMethod = assemblyEntryPointType.Methods.FirstOrDefault(x =>
                x.Name == ASSEMBLY_ENTRY_POINT_METHOD
            );
            if (assemblyEntryPointMethod == null || !assemblyEntryPointMethod.HasBody)
                error("Could not find entry point method in game: "
                      + ASSEMBLY_ENTRY_POINT_TYPE + "." + ASSEMBLY_ENTRY_POINT_METHOD);
            
            log("Finding instruction...");
            ILProcessor methodILProcessor = assemblyEntryPointMethod.Body.GetILProcessor();
            Instruction entryPoint = methodILProcessor.Create(OpCodes.Call, loaderEntryPointMethod);
            
            // Check if the entry point has already been injected
            if (findMatchingInstruction(assemblyEntryPointMethod.Body.Instructions, entryPoint) != -1)
                error("Entry point already found, the loader has already been installed!");
            
            FieldDefinition searchField = assemblyEntryPointType.Fields.Single(x => x.Name == "wasInMultiplayer");
            Instruction entryPointIndicator = methodILProcessor.Create(OpCodes.Stsfld, searchField);
            
            int entryPointIndex =
                findMatchingInstruction(assemblyEntryPointMethod.Body.Instructions, entryPointIndicator);
            if (entryPointIndex < 0) error("Could not find entry point instruction");
            
            log("Injecting entry point");
            assemblyEntryPointMethod.Body.Instructions.Insert(entryPointIndex + 1, entryPoint);
            // We should insert instructions to load any arguments we need here

            // Temporary dll path
            string tmpPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "tmp.dll";
            log("Saving to " + tmpPath);
            // Write assembly changes
            assemblyModule.Write(tmpPath);
            // Backup original assembly
            if (File.Exists(assemblyFilePath.FullName))
            {
                log("Backing up game assembly to " + assemblyFilePath + ".old");
                File.Move(assemblyFilePath.FullName, assemblyFilePath + ".old");
            }
            // Move modified assembly
            log("Moving new assembly");
            File.Move(tmpPath, assemblyFilePath.FullName);
            
            Logger.Info("Installer", "Installation complete\nPress any key to continue");
            Console.ReadLine();
        }

        private static void onLoggerLog(LogLevel level, string module, string msg)
        {
            XTERM.WriteLine(msg);
        }

        private static FileInfo getAssemblyFilePath()
        {
            DirectoryInfo dirInfo = getSteamDirectory();
            log("Found steam directory at " + dirInfo.FullName);
            dirInfo = dirInfo.GetDirectories().FirstOrDefault(x => x.Name == GAME_DIR_NAME);
            if (dirInfo == null) return null;
            log("Found " + dirInfo.FullName);
            dirInfo = dirInfo.GetDirectories().FirstOrDefault(x => x.Name == GAME_DIR_NAME + "_Data");
            if (dirInfo == null) return null;
            log("Found " + dirInfo.FullName);
            dirInfo = dirInfo.GetDirectories().FirstOrDefault(x => x.Name == "Managed");
            if (dirInfo == null) return null;
            log("Found " + dirInfo.FullName);
            FileInfo asmFile = dirInfo.GetFiles().FirstOrDefault(x => x.Name == "Assembly-CSharp.dll");
            return asmFile;
        }

        private static DirectoryInfo getSteamDirectory()
        {
            bool isUnix = Environment.OSVersion.Platform == PlatformID.Unix
                || Environment.OSVersion.Platform == PlatformID.MacOSX;
            string homeDirectory = (isUnix
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%")) + Path.DirectorySeparatorChar;

            string preSteamDirectory = isUnix
                ? string.Format(".local{0}share{0}Steam{0}steamapps{0}common{0}", Path.DirectorySeparatorChar)
                : string.Format("Program Files{0}Steam{0}steamapps{0}common{0}", Path.DirectorySeparatorChar);

            string steamDirectory = homeDirectory + preSteamDirectory;

            if (!Directory.Exists(steamDirectory))
            {
                steamDirectory = getUserInput("Could not detect steam directory" + Environment.NewLine
                                 + "(Tried " + steamDirectory + ")" + Environment.NewLine
                                 + "Please enter absolute steam directory");
            }
            
            return new DirectoryInfo(steamDirectory);
        }

        private static string getUserInput(string query)
        {
            XTERM.WriteLine(query);
            return Console.ReadLine();
        }

        private static bool canOpenFile(FileInfo file)
        {
            return canOpenFile(file.FullName);
        }
        
        private static bool canOpenFile(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // TODO: Log exception
                return false;
            }
        }
        
        private static string getLoaderFilePath()
        {
            // TODO: Don't assume loader DLL is in same directory as installer
            return AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + LOADER_ASSEMBLY_NAME;
        }

        private static void error(string message)
        {
            error(new Exception(message));
        }
        private static void error(Exception ex)
        {
            Logger.Error("Installer", ex);
        }
        private static void log(string message)
        {
            Logger.Info("Installer", message);
        }
        
                /// <summary>
        /// Returns the index of an instruction within a collection of instructions.
        /// Returns -1 if the collection does not contain the instruction
        /// </summary>
        /// <param name="instructions">The collection to search through</param>
        /// <param name="search">The instruction to search for</param>
        /// <returns></returns>
        private static int findMatchingInstruction(Mono.Collections.Generic.Collection<Instruction> instructions, Instruction search)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction o = instructions[i];
                if (compareInstructions(o, search))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Compares 2 instructions by value
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static bool compareInstructions(Instruction A, Instruction B)
        {
            if (A == null ^ B == null) return false;
            if (A == B) return true;
            if (!compareOpCodes(A.OpCode, B.OpCode)) return false;
            if (!compareOperands(A.Operand, B.Operand)) return false;

            return true;
        }

        private static bool compareOpCodes(OpCode A, OpCode B)
        {
            if (A == null ^ B == null) return false;
            if (A.Code != B.Code) return false;

            return true;
        }

        private static bool compareOperands(object A, object B)
        {
            if (A == null ^ B == null) return false;
            if (A == B) return true;
            Type Aty = A.GetType();
            Type Bty = B.GetType();
            if (string.Compare(Aty.FullName, Bty.FullName) != 0) return false;
            if (Aty == typeof(TypeReference))
            {
                var a = (TypeReference)A;
                var b = (TypeReference)B;
                if (string.Compare(a.FullName, b.FullName) != 0) return false;
            }
            else if (Aty == typeof(FieldReference))
            {
                var a = (FieldReference)A;
                var b = (FieldReference)B;
                if (string.Compare(a.FullName, b.FullName) != 0) return false;
            }
            else if (Aty == typeof(MethodReference))
            {
                var a = (MethodReference)A;
                var b = (MethodReference)B;
                if (string.Compare(a.FullName, b.FullName) != 0) return false;
            }
            else if (Aty == typeof(VariableDefinition))
            {
                var a = (VariableDefinition)A;
                var b = (VariableDefinition)B;
                if (a.VariableType.FullName == b.VariableType.FullName) return true;
                if (a.Name.Length > 0 && b.Name.Length > 0 && string.Compare(a.Name, b.Name) == 0) return true;

                return false;
            }
            else if (Aty == typeof(Instruction))
            {
                var a = (Instruction)A;
                var b = (Instruction)B;
                if (!compareOpCodes(a.OpCode, b.OpCode)) return false;
                if (!compareOperands(a.Operand, b.Operand)) return false;

                return true;
            }
            else
            {
                if (!A.Equals(B)) return false;
            }

            return true;
        }
    }
}