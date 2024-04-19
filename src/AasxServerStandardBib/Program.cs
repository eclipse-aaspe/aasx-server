﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using AasOpcUaServer;
using AasxMqttServer;
using AasxRestServerLibrary;
using AasxTimeSeries;
using AdminShellNS;
using Extensions;
using Jose;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using SampleClient;
using Environment = System.Environment;
using File = System.IO.File;
using Formatting = Newtonsoft.Json.Formatting;
using IReference = AasCore.Aas3_0.IReference;
using ReferenceTypes = AasCore.Aas3_0.ReferenceTypes;
using Timer = System.Timers.Timer;

/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    /// <summary>
    /// Checks whether the console will persist after the program exits.
    /// This should run only on Windows as it depends on kernel32.dll.
    ///
    /// The code has been adapted from: https://stackoverflow.com/a/63135555/1600678
    /// </summary>
    public static class WindowsConsoleWillBeDestroyedAtTheEnd
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

        public static bool Check()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }
    }

    public static class Program
    {
        public static IConfiguration Configuration { get; set; }

        private static void SaveEnvironment(int envIndex)
        {
            Console.WriteLine("SAVE: " + envFileName[envIndex]);
            var requestedFileName = envFileName[envIndex];
            var copyFileName = Path.GetTempFileName().Replace(".tmp", ".aasx");
            File.Copy(requestedFileName, copyFileName, true);
            env[envIndex].SaveAs(copyFileName);
            File.Copy(copyFileName, requestedFileName, true);
            File.Delete(copyFileName);
        }

        private static int _oldest;

        public static void LoadPackageForAas(string aasIdentifier, out IAssetAdministrationShell output, out int packageIndex)
        {
            output = null;
            packageIndex = -1;
            if (!withDb) return;
            if (isLoading) return;

            var i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var aas = env[i].AasEnv.AssetAdministrationShells.Where(a => a.Id.Equals(aasIdentifier));
                if (aas.Any())
                {
                    output = aas.First();
                    packageIndex = i;
                    return;
                }

                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = _oldest++;
                if (_oldest == env.Length)
                    _oldest = envimin;
            }

            using var db = new AasContext();
            var aasDBList = db.AasSets.Where(a => a.AasId == aasIdentifier);
            if (!aasDBList.Any())
            {
                return;
            }

            lock (changeAasxFile)
            {
                if (env[i] != null)
                {
                    Console.WriteLine("UNLOAD: " + envFileName[i]);
                    if (env[i].getWrite())
                    {
                        SaveEnvironment(i);
                        env[i].setWrite(false);
                    }

                    env[i].Close();
                }

                var aasDB = aasDBList.First();
                var aasxNum = aasDB.AASXNum;
                var aasxDBList = db.AASXSets.Where(a => a.AASXNum == aasxNum);
                var aasxDB = aasxDBList.First();
                var fn = aasxDB.AASX;
                envFileName[i] = fn;

                if (!withDbFiles)
                {
                    Console.WriteLine("LOAD: " + fn);
                    env[i] = new AdminShellPackageEnv(fn);

                    var timeStamp = DateTime.Now;
                    var a = env[i].AasEnv.AssetAdministrationShells[0];
                    a.TimeStampCreate = timeStamp;
                    a.SetTimeStamp(timeStamp);
                    foreach (var submodel in env[i].AasEnv.Submodels)
                    {
                        submodel.TimeStampCreate = timeStamp;
                        submodel.SetTimeStamp(timeStamp);
                        submodel.SetAllParents(timeStamp);
                    }

                    output = a;
                }
                else
                {
                    Console.WriteLine("LOAD: " + aasDB.Idshort);
                    env[i] = new AdminShellPackageEnv();
                    env[i].SetFilename(fn);

                    var aas = new AssetAdministrationShell(
                        id: aasDB.AasId,
                        idShort: aasDB.Idshort,
                        assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId)
                    );
                    env[i].AasEnv.AssetAdministrationShells.Add(aas);

                    var submodelDBList = db.SubmodelSets
                        .OrderBy(sm => sm.SubmodelNum)
                        .Where(sm => sm.AasNum == aasDB.AasNum)
                        .ToList();

                    aas.Submodels = new List<IReference>();
                    foreach (var sm in submodelDBList.Select(submodelDB => DBRead.getSubmodel(submodelDB.SubmodelId)))
                    {
                        aas.Submodels.Add(sm.GetReference());
                        env[i].AasEnv.Submodels.Add(sm);
                    }

                    output = aas;
                }

                packageIndex = i;
                SignalNewData(2);
            }
        }

        public static void LoadPackageForSubmodel(string submodelIdentifier, out ISubmodel output, out int packageIndex)
        {
            output = null;
            packageIndex = -1;
            if (!withDb) return;
            if (isLoading) return;

            var i = envimin;
            while (i < env.Length)
            {
                if (env[i] == null)
                    break;

                var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                if (submodels.Any())
                {
                    output = submodels.First();
                    packageIndex = i;
                    return;
                }

                i++;
            }

            // not found in memory
            if (i == env.Length)
            {
                i = _oldest++;
                if (_oldest == env.Length)
                    _oldest = envimin;
            }

            using var db = new AasContext();
            var submodelDBList = db.SubmodelSets.Where(s => s.SubmodelId == submodelIdentifier);
            if (!submodelDBList.Any())
            {
                return;
            }

            lock (changeAasxFile)
            {
                if (env[i] != null)
                {
                    Console.WriteLine("UNLOAD: " + envFileName[i]);
                    if (env[i].getWrite())
                    {
                        SaveEnvironment(i);
                        env[i].setWrite(false);
                    }

                    env[i].Close();
                }

                var submodelDB = submodelDBList.First();
                var aasxNum = submodelDB.AASXNum;
                var aasxDBList = db.AASXSets.Where(a => a.AASXNum == aasxNum);
                var aasxDB = aasxDBList.First();
                var fn = aasxDB.AASX;
                envFileName[i] = fn;

                if (!withDbFiles)
                {
                    Console.WriteLine("LOAD: " + fn);
                    env[i] = new AdminShellPackageEnv(fn);

                    var timeStamp = DateTime.Now;
                    var a = env[i].AasEnv.AssetAdministrationShells[0];
                    a.TimeStampCreate = timeStamp;
                    a.SetTimeStamp(timeStamp);
                    foreach (var submodel in env[i].AasEnv.Submodels)
                    {
                        submodel.TimeStampCreate = timeStamp;
                        submodel.SetTimeStamp(timeStamp);
                        submodel.SetAllParents(timeStamp);
                    }

                    var submodels = env[i].AasEnv.Submodels.Where(s => s.Id.Equals(submodelIdentifier));
                    if (submodels.Any())
                    {
                        output = submodels.First();
                    }
                }
                else
                {
                    Console.WriteLine("LOAD Submodel: " + submodelDB.Idshort);
                    env[i] = new AdminShellPackageEnv();
                    env[i].SetFilename(fn);

                    var aasDBList = db.AasSets.Where(a => a.AASXNum == submodelDB.AASXNum);
                    var aasDB = aasDBList.First();

                    var aas = new AssetAdministrationShell(
                        id: aasDB.AasId,
                        idShort: aasDB.Idshort,
                        assetInformation: new AssetInformation(AssetKind.Type, aasDB.AssetId)
                    );
                    env[i].AasEnv.AssetAdministrationShells.Add(aas);

                    var submodelDBList2 = db.SubmodelSets
                        .OrderBy(sm => sm.SubmodelNum)
                        .Where(sm => sm.AasNum == aasDB.AasNum)
                        .ToList();

                    aas.Submodels = new List<IReference>();
                    foreach (var sDB in submodelDBList2)
                    {
                        var sm = DBRead.getSubmodel(sDB.SubmodelId);
                        aas.Submodels.Add(sm.GetReference());
                        env[i].AasEnv.Submodels.Add(sm);
                        if (sDB.SubmodelId == submodelIdentifier)
                            output = sm;
                    }
                }

                packageIndex = i;
                SignalNewData(2);
            }
        }

        public static int envimin;
        public static int envimax = 200;
        public static AdminShellPackageEnv[] env;
        public static string[] envFileName;
        public static string[] envSymbols;
        public static string[] envSubjectIssuer;

        public static string hostPort = "";
        public static string blazorPort = "";
        public static string blazorHostPort = "";
        public static ulong dataVersion;

        public static void ChangeDataVersion()
        {
            dataVersion++;
        }

        public static ulong GetDataVersion()
        {
            return (dataVersion);
        }

        static Dictionary<string, UASampleClient> OPCClients = new Dictionary<string, UASampleClient>();
        static readonly object opcclientAddLock = new object(); // object for lock around connecting to an external opc server

        static MqttServer AASMqttServer = new MqttServer();

        static bool runOPC;

        public static string connectServer = "";
        public static string connectNodeName = "";
        static int connectUpdateRate = 1000;
        static Thread connectThread;
        static bool connectLoop;

        public static WebProxy proxy;
        public static HttpClientHandler clientHandler;

        public static bool noSecurity;
        public static bool edit;
        public static string externalRest = "";
        public static string externalBlazor = "";
        public static bool readTemp;
        public static int saveTemp;
        public static DateTime saveTempDt = new DateTime();
        public static string secretStringAPI;
        public static bool htmlId;
        public static long submodelAPIcount = 0;

        public static HashSet<object> submodelsToPublish = new HashSet<object>();
        public static HashSet<object> submodelsToSubscribe = new HashSet<object>();

        public static Dictionary<object, string> generatedQrCodes = new Dictionary<object, string>();

        public static string redirectServer = "";
        public static string authType = "";
        public static string getUrl = "";
        public static string getSecret = "";

        public static bool isLoading = true;
        public static int count = 0;

        public static bool initializingRegistry = false;

        public static object changeAasxFile = new object();

        public static Dictionary<string, string> envVariables = new Dictionary<string, string>();

        public static bool withDb;
        public static bool isPostgres;
        public static bool withDbFiles;
        public static int startIndex;

        public static bool withPolicy;

        public static bool showWeight = false;

        private class CommandLineArguments
        {
            public string Host { get; set; }
            public string Port { get; set; }
            public bool Https { get; set; }
            public string DataPath { get; set; }
            public bool Rest { get; set; }
            public bool Opc { get; set; }
            public bool Mqtt { get; set; }
            public bool DebugWait { get; set; }
            public int? OpcClientRate { get; set; }
            public string[] Connect { get; set; }
            public string ProxyFile { get; set; }
            public bool NoSecurity { get; set; }
            public bool Edit { get; set; }
            public string Name { get; set; }
            public string ExternalRest { get; set; }
            public string ExternalBlazor { get; set; }
            public bool ReadTemp { get; set; }
            public int SaveTemp { get; set; }
            public string SecretStringAPI { get; set; }
            public string Tag { get; set; }
            public bool HtmlId { get; set; }
            public int AasxInMemory { get; set; }
            public bool WithDb { get; set; }
            public bool NoDbFiles { get; set; }
            public int StartIndex { get; set; }
        }

        private static async Task<int> Run(CommandLineArguments a)
        {
            // Wait for Debugger
            if (a.DebugWait)
            {
                Console.WriteLine("Please attach debugger now to {0}!", a.Host);
                while (!Debugger.IsAttached)
                    Thread.Sleep(100);
                Console.WriteLine("Debugger attached");
            }

            // Read environment variables
            string[] evlist = {"PLCNEXTTARGET", "WITHPOLICY", "SHOWWEIGHT"};
            foreach (var ev in evlist)
            {
                var v = Environment.GetEnvironmentVariable(ev);
                if (v == null)
                {
                    continue;
                }

                v = v.Replace("\r", "");
                v = v.Replace("\n", "");
                Console.WriteLine("Variable: " + ev + " = " + v);
                envVariables.Add(ev, v);
            }

            if (envVariables.TryGetValue("WITHPOLICY", out var w))
            {
                withPolicy = w.ToLower() switch
                {
                    "true" or "on" => true,
                    "false" or "off" => false,
                    _ => withPolicy
                };

                Console.WriteLine("withPolicy: " + withPolicy);
            }
            if (envVariables.TryGetValue("SHOWWEIGHT", out w))
            {
                if (w.ToLower() == "true" || w.ToLower() == "on")
                {
                    showWeight = true;
                }
                if (w.ToLower() == "false" || w.ToLower() == "off")
                {
                    showWeight = false;
                }
                Console.WriteLine("showWeight: " + showWeight);
            }

            if (a.Connect != null)
            {
                switch (a.Connect.Length)
                {
                    case 0:
                    {
                        connectServer = "http://admin-shell-io.com:52000";
                        var barray = new byte[10];
                        RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                        rngCsp.GetBytes(barray);
                        connectNodeName = "AasxServer_" + Convert.ToBase64String(barray);
                        connectUpdateRate = 2000;
                        if (a.Name != null && a.Name != "")
                            connectNodeName = a.Name;
                        break;
                    }
                    case 1:
                    {
                        var parsable = true;

                        var c = a.Connect[0].Split(',');
                        if (c.Length == 3)
                        {
                            var rate = 0;
                            try
                            {
                                rate = Convert.ToInt32(c[2]);
                            }
                            catch (FormatException)
                            {
                                parsable = false;
                            }

                            if (parsable)
                            {
                                if (c[0].Length == 0 || c[1].Length == 0 || c[2].Length == 0 || rate <= 0)
                                {
                                    parsable = false;
                                }
                                else
                                {
                                    connectServer = c[0];
                                    connectNodeName = c[1];
                                    connectUpdateRate = Convert.ToInt32(c[2]);
                                }
                            }
                        }
                        else
                        {
                            parsable = false;
                        }

                        if (!parsable)
                        {
                            await Console.Error.WriteLineAsync(
                                $"Invalid --connect. Expected a comma-separated values (server, node name, period in milliseconds), but got: {a.Connect[0]}");
                            return 1;
                        }

                        break;
                    }
                }

                Console.WriteLine($"--connect: ConnectServer {connectServer}, NodeName {connectNodeName}, UpdateRate {connectUpdateRate}");
            }

            if (a.DataPath != null)
            {
                Console.WriteLine($"Serving the AASXs from: {a.DataPath}");
                AasxHttpContextHelper.DataPath = a.DataPath;
            }

            runOPC = a.Opc;
            noSecurity = a.NoSecurity;
            edit = a.Edit;
            readTemp = a.ReadTemp;
            saveTemp = a.SaveTemp;
            htmlId = a.HtmlId;
            withDb = a.WithDb;
            withDbFiles = a.WithDb;
            if (a.NoDbFiles)
                withDbFiles = false;
            if (a.StartIndex > 0)
                startIndex = a.StartIndex;
            if (a.AasxInMemory > 0)
                envimax = a.AasxInMemory;
            if (!string.IsNullOrEmpty(a.SecretStringAPI))
            {
                secretStringAPI = a.SecretStringAPI;
                Console.WriteLine($"secretStringAPI = {secretStringAPI}");
            }

            if (a.OpcClientRate is < 200)
            {
                Console.WriteLine("Recommend an OPC client update rate > 200 ms.");
            }

            // allocate memory
            lock (changeAasxFile)
            {
                env = new AdminShellPackageEnv[envimax];
            }

            envFileName = new string[envimax];
            envSymbols = new string[envimax];
            envSubjectIssuer = new string[envimax];

            // Proxy
            var proxyAddress = string.Empty;
            var username = string.Empty;
            var password = string.Empty;

            if (a.ProxyFile != null)
            {
                if (!File.Exists(a.ProxyFile))
                {
                    await Console.Error.WriteLineAsync($"Proxy file not found: {a.ProxyFile}");
                    return 1;
                }

                try
                {
                    using var streamReader = new StreamReader(a.ProxyFile);
                    proxyAddress = await streamReader.ReadLineAsync();
                    username = await streamReader.ReadLineAsync();
                    password = await streamReader.ReadLineAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine($"{a.ProxyFile} not found!");
                }

                if (!string.IsNullOrEmpty(proxyAddress))
                {
                    proxy = new WebProxy();
                    var newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    proxy.Credentials = new NetworkCredential(username, password);
                    Console.WriteLine("Using proxy: " + proxyAddress);

                    clientHandler = new HttpClientHandler
                    {
                        Proxy = proxy,
                        UseProxy = true
                    };
                }
            }

            hostPort = $"{a.Host}:{a.Port}";
            blazorHostPort = $"{a.Host}:{blazorPort}";

            if (a.ExternalRest != null)
            {
                externalRest = a.ExternalRest;
            }
            else
            {
                externalRest = $"http://{hostPort}";
            }

            externalBlazor = a.ExternalBlazor ?? blazorHostPort;

            // Pass global options to subprojects
            AdminShellPackageEnv.setGlobalOptions(withDb, withDbFiles, a.DataPath);

            // Read root cert from root subdirectory
            Console.WriteLine("Security 1 Startup - Server");
            Console.WriteLine("Security 1.1 Load X509 Root Certificates into X509 Store Root");

            var root = new X509Store("Root", StoreLocation.CurrentUser);
            root.Open(OpenFlags.ReadWrite);

            var ParentDirectory = new DirectoryInfo(".");

            if (Directory.Exists("./root"))
            {
                foreach (var f in ParentDirectory.GetFiles("./root/*.cer"))
                {
                    var cert = new X509Certificate2($"./root/{f.Name}");

                    root.Add(cert);
                    Console.WriteLine("Security 1.1 Add " + f.Name);
                }
            }

            if (!Directory.Exists("./temp"))
                Directory.CreateDirectory("./temp");

            if (a.Opc)
            {
                var isBaseAddresses = false;
                var isUaString = false;
                var reader = new XmlTextReader("Opc.Ua.SampleServer.Config.xml");
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            switch (reader.Name)
                            {
                                case "BaseAddresses":
                                    isBaseAddresses = true;
                                    break;
                                case "ua:String":
                                    isUaString = true;
                                    break;
                            }

                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            if (isBaseAddresses && isUaString)
                            {
                                Console.WriteLine("Connect to OPC UA by: {0}", reader.Value);
                                isBaseAddresses = false;
                                isUaString = false;
                            }

                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            break;
                        case XmlNodeType.None:
                        case XmlNodeType.Attribute:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.EntityReference:
                        case XmlNodeType.Entity:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Document:
                        case XmlNodeType.DocumentType:
                        case XmlNodeType.DocumentFragment:
                        case XmlNodeType.Notation:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.EndEntity:
                        case XmlNodeType.XmlDeclaration:
                        default:
                            throw new ArgumentOutOfRangeException(nameof(reader.NodeType));
                    }
                }
            }

            var createFilesOnly = File.Exists(AasxHttpContextHelper.DataPath + "/FILES.ONLY");

            var envi = 0;
            var count = 0;

            // Migrate always
            if (withDb)
            {
                if (isPostgres)
                {
                    Console.WriteLine("Use POSTGRES");
                    await using var db = new PostgreAasContext();
                    await db.Database.MigrateAsync();
                }
                else
                {
                    Console.WriteLine("Use SQLITE");
                    await using var db = new SqliteAasContext();
                    await db.Database.MigrateAsync();
                }
            }

            // Clear DB
            if (withDb && startIndex == 0 && !createFilesOnly)
            {
                await using var db = new AasContext();
                var task = Task.Run(async () => count = await db.DbConfigSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.AASXSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.AasSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.SubmodelSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.SMESets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.IValueSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.SValueSets.ExecuteDeleteAsync());
                task.Wait();
                task = Task.Run(async () => count = await db.DValueSets.ExecuteDeleteAsync());
                task.Wait();

                var dbConfig = new DbConfigSet
                {
                    Id = 1,
                    AasCount = 0,
                    SubmodelCount = 0,
                    AASXCount = 0,
                    SMECount = 0
                };
                db.Add(dbConfig);
                await db.SaveChangesAsync();
            }

            if (Directory.Exists(AasxHttpContextHelper.DataPath))
            {
                if (!Directory.Exists($"{AasxHttpContextHelper.DataPath}/xml"))
                    Directory.CreateDirectory($"{AasxHttpContextHelper.DataPath}/xml");
                if (!Directory.Exists($"{AasxHttpContextHelper.DataPath}/files"))
                    Directory.CreateDirectory($"{AasxHttpContextHelper.DataPath}/files");

                var watch = Stopwatch.StartNew();
                var fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx");
                Array.Sort(fileNames);

                var maxTasks = 1;
                var taskIndex = 0;

                var fi = 0;
                string fn;
                while (fi < fileNames.Length)
                {
                    fn = fileNames[fi];
                    if (fn.ToLower().Contains("globalsecurity"))
                    {
                        envFileName[envi] = fn;
                        env[envi] = new AdminShellPackageEnv(fn, true);
                        envi++;
                        envimin = envi;
                        _oldest = envi;
                        fi++;
                        continue;
                    }

                    if (fi < startIndex)
                    {
                        fi++;
                        continue;
                    }


                    if (!string.IsNullOrEmpty(fn) && envi < envimax)
                    {
                        var name = Path.GetFileName(fn);
                        var tempName = $"./temp/{Path.GetFileName(fn)}";

                        // Convert to the newest version only
                        if (saveTemp == -1)
                        {
                            env[envi] = new AdminShellPackageEnv(fn, true);
                            if (env[envi] == null)
                            {
                                await Console.Error.WriteLineAsync($"Cannot open {fn}. Aborting..");
                                return 1;
                            }

                            Console.WriteLine($"{(fi + 1)}/{fileNames.Length} {watch.ElapsedMilliseconds / 1000}s SAVE TO TEMP: {fn}");
                            env[envi].SaveAs(tempName);
                            fi++;
                            continue;
                        }

                        if (readTemp && File.Exists(tempName))
                        {
                            fn = tempName;
                        }

                        Console.WriteLine("{1}/{2} {3}s" + " Loading {0}...", fn, (fi + 1), fileNames.Length, watch.ElapsedMilliseconds / 1000);
                        envFileName[envi] = fn;
                        if (!withDb)
                        {
                            env[envi] = new AdminShellPackageEnv(fn, true);
                            if (env[envi] == null)
                            {
                                await Console.Error.WriteLineAsync($"Cannot open {fn}. Aborting..");
                                return 1;
                            }
                        }
                        else
                        {
                            envFileName[envi] = null;
                            env[envi] = null;

                            using var asp = new AdminShellPackageEnv(fn, false, true);
                            if (!createFilesOnly)
                            {
                                await using (var db = new AasContext())
                                {
                                    var configDBList = db.DbConfigSets.Where(d => true);
                                    var dbConfig = configDBList.FirstOrDefault();

                                    var aasxNum = ++dbConfig.AASXCount;
                                    var aasxDB = new AASXSet
                                    {
                                        AASXNum = aasxNum,
                                        AASX = fn
                                    };
                                    db.Add(aasxDB);

                                    var aas = asp.AasEnv.AssetAdministrationShells[0];
                                    var aasId = aas.Id;
                                    var assetId = aas.AssetInformation.GlobalAssetId;

                                    // Check security
                                    if (!string.IsNullOrEmpty(aasId) && !string.IsNullOrEmpty(assetId))
                                    {
                                        VisitorAASX.LoadAASInDB(db, aas, aasxNum, asp, dbConfig);
                                    }

                                    await db.SaveChangesAsync();
                                }
                            }

                            if (withDbFiles)
                            {
                                try
                                {
                                    var fcopyt = $"{name}__thumbnail";
                                    fcopyt = fcopyt.Replace("/", "_");
                                    fcopyt = fcopyt.Replace(".", "_");
                                    Uri dummy = null;
                                    await using var st = asp.GetLocalThumbnailStream(ref dummy, init: true);
                                    Console.WriteLine($"Copy {AasxHttpContextHelper.DataPath}/files/{fcopyt}.dat");
                                    var fst = File.Create($"{AasxHttpContextHelper.DataPath}/files/{fcopyt}.dat");
                                    await st?.CopyToAsync(fst);
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception.Message);
                                }

                                await using var fileStream = new FileStream($"{AasxHttpContextHelper.DataPath}/files/{name}.zip", FileMode.Create);
                                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
                                var files = asp.GetListOfSupplementaryFiles();
                                foreach (var f in files)
                                {
                                    try
                                    {
                                        await using var s = asp.GetLocalStreamFromPackage(f.Uri.OriginalString, init: true);
                                        var archiveFile = archive.CreateEntry(f.Uri.OriginalString);
                                        Console.WriteLine($"Copy {AasxHttpContextHelper.DataPath}/{name}/{f.Uri.OriginalString}");

                                        await using var archiveStream = archiveFile.Open();
                                        await s.CopyToAsync(archiveStream);
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(exception.Message);
                                    }
                                }
                            }
                        }

                        // check if signed
                        var fileCert = $"./user/{name}.cer";
                        if (File.Exists(fileCert))
                        {
                            var x509 = new X509Certificate2(fileCert);
                            envSymbols[envi] = "S";
                            envSubjectIssuer[envi] = x509.Subject;

                            var chain = new X509Chain();
                            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                            var isValid = chain.Build(x509);
                            if (isValid)
                            {
                                envSymbols[envi] += ";V";
                                envSubjectIssuer[envi] += ";" + x509.Issuer;
                            }
                        }
                    }

                    fi++;
                    envi++;
                }

                if (saveTemp == -1)
                    return (0);

                watch.Stop();
                Console.WriteLine($"{fi} AASX loaded in {watch.ElapsedMilliseconds / 1000}s");

                fileNames = Directory.GetFiles(AasxHttpContextHelper.DataPath, "*.aasx2");
                Array.Sort(fileNames);

                foreach (var t in fileNames)
                {
                    fn = t;

                    if (fn != "" && envi < envimax)
                    {
                        envFileName[envi] = fn;
                        envSymbols[envi] = "L"; // Show lock
                    }

                    envi++;
                }
            }

            Console.WriteLine("Please wait for the servers to start...");

            if (a.Rest)
            {
                Console.WriteLine("--rest argument is not supported anymore, as the old V2 related REST APIs are deprecated. Please find the new REST APIs on the port 5001.");
            }

            TimeSeries.timeSeriesInit();

            AasxTask.taskInit();

            RunScript(true);

            isLoading = false;

            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStartAsync().Wait();
                Console.WriteLine("MQTT Publisher started.");
            }

            MySampleServer server = null;
            if (a.Opc)
            {
                server = new MySampleServer(_autoAccept: true, _stopTimeout: 0, _aasxEnv: env);
                Console.WriteLine("OPC UA Server started..");
            }

            if (a.OpcClientRate != null) // read data by OPC UA
            {
                // Initial read of OPC values, will quit the program if it returns false
                if (!ReadOPCClient(true))
                {
                    await Console.Error.WriteLineAsync("Failed to read from the OPC client.");
                    return 1;
                }

                Console.WriteLine($"OPC client will be updating every: {a.OpcClientRate} milliseconds");
                SetOPCClientTimer((double) a.OpcClientRate); // read again everytime timer expires
            }

            SetScriptTimer(1000); // also updates blazor view

            if (!string.IsNullOrEmpty(connectServer))
            {
                if (clientHandler == null)
                {
                    clientHandler = new HttpClientHandler();
                    clientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                }

                var httpClient = new HttpClient(clientHandler);

                var payload = $"{{ \"source\" : \"{connectNodeName}\" }}";

                var content = string.Empty;
                try
                {
                    var contentJson = new StringContent(payload, Encoding.UTF8, "application/json");
                    var result = httpClient.PostAsync($"{connectServer}/connect", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                if (content == "OK")
                {
                    connectThread = new Thread(ConnectThreadLoop);
                    connectLoop = true;
                }
                else
                {
                    Console.WriteLine($"********** Can not connect to: {connectServer}");
                }
            }

            SignalNewData(3);

            if (a.Opc && server != null)
            {
                server.Run(); // wait for CTRL-C
            }
            else
            {
                // no OPC UA: wait only for CTRL-C
                Console.WriteLine("Servers successfully started. Press Ctrl-C to exit...");
            }

            // wait for RETURN
            if (!string.IsNullOrEmpty(connectServer) && connectLoop)
            {
                connectLoop = false;
            }

            if (a.Mqtt)
            {
                AASMqttServer.MqttSeverStopAsync().Wait();
            }

            AasxRestServer.Stop();

            return 0;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("args:");
            foreach (var a in args)
            {
                Console.WriteLine(a);
            }

            Console.WriteLine();

            AasContext._con = Configuration;
            if (Configuration?["DatabaseConnection:ConnectionString"] != null)
            {
                isPostgres = Configuration["DatabaseConnection:ConnectionString"].ToLower().Contains("host");
            }

            var rootCommand = new RootCommand("serve AASX packages over different interfaces")
            {
                new Option<string>(
                    new[] {"--host"},
                    () => "localhost",
                    "Host which the server listens on"),

                new Option<string>(
                    new[] {"--data-path"},
                    "Path to where the AASXs reside"),

                new Option<bool>(
                    new[] {"--opc"},
                    "If set, starts the OPC server"),

                new Option<bool>(
                    new[] {"--mqtt"},
                    "If set, starts a MQTT publisher"),

                new Option<bool>(
                    new[] {"--debug-wait"},
                    "If set, waits for Debugger to attach"),

                new Option<int>(
                    new[] {"--opc-client-rate"},
                    "If set, starts an OPC client and refreshes on the given period " +
                    "(in milliseconds)"),

                new Option<string>(
                    new[] {"--proxy-file"},
                    "If set, parses the proxy information from the given proxy file"),

                new Option<bool>(
                    new[] {"--no-security"},
                    "If set, no authentication is required"),

                new Option<bool>(
                    new[] {"--edit"},
                    "If set, allows edits in the user interface"),

                new Option<string>(
                    new[] {"--name"},
                    "Name of the server"),

                new Option<string>(
                    new[] {"--external-blazor"},
                    "external name of the server blazor UI"),

                new Option<bool>(
                    new[] {"--read-temp"},
                    "If set, reads existing AASX from temp at startup"),

                new Option<int>(
                    new[] {"--save-temp"},
                    "If set, writes AASX every given seconds"),

                new Option<string>(
                    new[] {"--secret-string-api"},
                    "If set, allows UPDATE access by query parameter s="),

                new Option<bool>(
                    new[] {"--html-id"},
                    "If set, creates id for HTML objects in blazor tree for testing"),

                new Option<string>(
                    new[] {"--tag"},
                    "Only used to differ servers in task list"),

                new Option<int>(
                    new[] {"--aasx-in-memory"},
                    "If set, size of array of AASX files in memory"),

                new Option<bool>(
                    new[] {"--with-db"},
                    "If set, will use DB by Entity Framework"),

                new Option<bool>(
                    new[] {"--no-db-files"},
                    "If set, do not export files from AASX into ZIP"),

                new Option<int>(
                    new[] {"--start-index"},
                    "If set, start index in list of AASX files")
            };

            if (args.Length == 0)
            {
                new HelpBuilder(new SystemConsole()).Write(rootCommand);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    !WindowsConsoleWillBeDestroyedAtTheEnd.Check())
                {
                    return;
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();

                return;
            }

            rootCommand.Handler = CommandHandler.Create((CommandLineArguments a) =>
            {
                var task = Run(a);
                task.Wait();
                var op = task.Result;
                return Task.FromResult(op);
            });

            var exitCode = rootCommand.InvokeAsync(args).Result;
            Environment.ExitCode = exitCode;
        }


        public static string ContentToString(this HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        public class AasListParameters
        {
            public int index;
            public string idShort;
            public string identification;
            public string fileName;
            public string assetId;
            public string humanEndPoint;
            public string restEndPoint;
        }

        public class AasDirectoryParameters
        {
            public string source;
            public List<AasListParameters> aasList;

            public AasDirectoryParameters()
            {
                aasList = new List<AasListParameters>();
            }
        }

        /* AAS Detail Part 2 Descriptor Definitions BEGIN*/
        /* End Point Definition */
        public class AASxEndpoint
        {
            [XmlElement(ElementName = "address")] public string address = string.Empty;
            [XmlElement(ElementName = "type")] public string type = string.Empty;
        }

        /* Submodel Descriptor Definition */
        public class SubmodelDescriptors
        {
            [XmlElement(ElementName = "administration")] [JsonIgnore]
            public AdministrativeInformation administration;

            [XmlElement(ElementName = "description")] [JsonIgnore]
            public List<ILangStringTextType> description;

            [XmlElement(ElementName = "idShort")] [JsonIgnore]
            public string idShort = string.Empty;

            [XmlElement(ElementName = "identification")] [JsonIgnore]
            public string identification;

            [XmlElement(ElementName = "semanticId")]
            public Reference semanticId;

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new();
        }

        /* AAS Descriptor Definiton */
        public class AasDescriptor
        {
            [XmlElement(ElementName = "administration")] [JsonIgnore]
            public AdministrativeInformation administration;

            [XmlElement(ElementName = "description")] [JsonIgnore]
            public List<ILangStringTextType> description = new(new List<ILangStringTextType>());

            [XmlElement(ElementName = "idShort")] public string idShort = string.Empty;

            [XmlElement(ElementName = "identification")] [JsonIgnore]
            public string identification;

            [XmlElement(ElementName = "assets")] public List<AssetInformation> assets = new();

            [XmlElement(ElementName = "endpoints")]
            public List<AASxEndpoint> endpoints = new();

            [XmlElement(ElementName = "submodelDescriptors")]
            public List<SubmodelDescriptors> submodelDescriptors = new();
        }

        /* AAS Detail Part 2 Descriptor Definitions END*/
        /* Creation of AAS Descriptor */
        // TODO (jtikekar, 2023-09-04): Remove for now
        public static AasDescriptor CreateAASDescriptor(AdminShellPackageEnv adminShell)
        {
            var aasD = new AasDescriptor();
            var endpointAddress = $"http://{hostPort}";

            aasD.idShort = adminShell.AasEnv.AssetAdministrationShells[0].IdShort;
            aasD.identification = adminShell.AasEnv.AssetAdministrationShells[0].Id;
            aasD.description = adminShell.AasEnv.AssetAdministrationShells[0].Description;

            var endp = new AASxEndpoint
            {
                address = $"{endpointAddress}/aas/{adminShell.AasEnv.AssetAdministrationShells[0].IdShort}"
            };
            aasD.endpoints.Add(endp);

            var submodelCount = adminShell.AasEnv.Submodels.Count;
            for (var i = 0; i < submodelCount; i++)
            {
                var sdc = new SubmodelDescriptors
                {
                    administration = adminShell.AasEnv.Submodels[i].Administration as AdministrativeInformation,
                    description = adminShell.AasEnv.Submodels[i].Description,
                    identification = adminShell.AasEnv.Submodels[i].Id,
                    idShort = adminShell.AasEnv.Submodels[i].IdShort,
                    semanticId = adminShell.AasEnv.Submodels[i].SemanticId as Reference
                };

                var endpSub = new AASxEndpoint
                {
                    address = $"{endpointAddress}/aas/{adminShell.AasEnv.AssetAdministrationShells[0].IdShort}/submodels/{adminShell.AasEnv.Submodels[i].IdShort}",
                    type = "http"
                };
                sdc.endpoints.Add(endpSub);

                aasD.submodelDescriptors.Add(sdc);
            }

            return aasD;
        }

        /*Publishing the AAS Descriptor*/
        public static void PublishDescriptorData(string descriptorData)
        {
            var httpClient = clientHandler != null ? new HttpClient(clientHandler) : new HttpClient();

            var descriptorJson = new StringContent(descriptorData, Encoding.UTF8, "application/json");
            try
            {
                _ = httpClient.PostAsync($"{connectServer}/publish", descriptorJson).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public class TransmitData
        {
            public string source;
            public string destination;
            public string type;
            public string encrypt;
            public string extensions;
            public List<string> publish = new();
        }

        public class TransmitFrame
        {
            public string source;
            public List<TransmitData> data = new();
        }

        static bool getDirectory = true;
        static string getDirectoryDestination = string.Empty;

        static string getaasxFile_destination = string.Empty;
        static string getaasxFile_fileName = string.Empty;
        static string getaasxFile_fileData = string.Empty;
        static string getaasxFile_fileType = string.Empty;
        static int getaasxFile_fileLenBase64;
        static int getaasxFile_fileLenBinary;
        static int getaasxFile_fileTransmitted;
        static int blockSize = 1500000;

        static List<TransmitData> tdPending = new();

        public static void connectPublish(string type, string json)
        {
            if (string.IsNullOrEmpty(connectServer))
                return;

            var tdp = new TransmitData
            {
                source = connectNodeName,
                type = type
            };

            tdp.publish.Add(json);
            tdPending.Add(tdp);
        }

        public static void ConnectThreadLoop()
        {
            while (connectLoop)
            {
                var tf = new TransmitFrame
                {
                    source = connectNodeName
                };
                TransmitData td;

                if (getDirectory)
                {
                    Console.WriteLine("if getDirectory");

                    // AAAS Detail part 2 Descriptor
                    var descriptortf = new TransmitFrame
                    {
                        source = connectNodeName
                    };

                    var adp = new AasDirectoryParameters
                    {
                        source = connectNodeName
                    };

                    var aascount = env.Length;

                    for (var j = 0; j < aascount; j++)
                    {
                        var alp = new AasListParameters();

                        if (env[j] == null)
                        {
                            continue;
                        }

                        alp.index = j;

                        /* Create Detail part 2 Descriptor Start */
                        var aasDsecritpor = CreateAASDescriptor(env[j]);
                        var aasDsecritporTData = new TransmitData
                        {
                            source = connectNodeName,
                            type = "register",
                            destination = "VWS_AAS_Registry"
                        };
                        var aasDescriptorJsonData = JsonConvert.SerializeObject(aasDsecritpor, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                        aasDsecritporTData.publish.Add(aasDescriptorJsonData);
                        descriptortf.data.Add(aasDsecritporTData);
                        /* Create Detail part 2 Descriptor END */

                        alp.idShort = env[j].AasEnv.AssetAdministrationShells[0].IdShort;
                        alp.identification = env[j].AasEnv.AssetAdministrationShells[0].Id;
                        alp.fileName = envFileName[j];
                        alp.assetId = string.Empty;
                        var asset = env[j].AasEnv.AssetAdministrationShells[0].AssetInformation;
                        if (asset != null)
                            alp.humanEndPoint = blazorHostPort;
                        alp.restEndPoint = hostPort;

                        adp.aasList.Add(alp);
                    }

                    var decriptorData = JsonConvert.SerializeObject(descriptortf, Formatting.Indented);
                    PublishDescriptorData(decriptorData);

                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    var json = JsonConvert.SerializeObject(adp, Formatting.Indented);
                    td.type = "directory";
                    td.destination = getDirectoryDestination;
                    td.publish.Add(json);
                    tf.data.Add(td);
                    Console.WriteLine("Send directory");

                    getDirectory = false;
                    getDirectoryDestination = string.Empty;
                }

                if (!string.IsNullOrEmpty(getaasxFile_destination)) // block transfer
                {
                    dynamic res = new ExpandoObject();

                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    int len;
                    if ((getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted) > blockSize)
                    {
                        len = blockSize;
                    }
                    else
                    {
                        len = getaasxFile_fileLenBase64 - getaasxFile_fileTransmitted;
                    }

                    res.fileData = getaasxFile_fileData.Substring(getaasxFile_fileTransmitted, len);
                    res.fileName = getaasxFile_fileName;
                    res.fileLenBase64 = getaasxFile_fileLenBase64;
                    res.fileLenBinary = getaasxFile_fileLenBinary;
                    res.fileType = getaasxFile_fileType;
                    res.fileTransmitted = getaasxFile_fileTransmitted;

                    string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                    td.destination = getaasxFile_destination;
                    td.type = "getaasxBlock";
                    td.publish.Add(responseJson);
                    tf.data.Add(td);

                    getaasxFile_fileTransmitted += len;

                    if (getaasxFile_fileTransmitted == getaasxFile_fileLenBase64)
                    {
                        getaasxFile_destination = string.Empty;
                        getaasxFile_fileName = string.Empty;
                        getaasxFile_fileData = string.Empty;
                        getaasxFile_fileType = string.Empty;
                        res.fileLenBase64 = 0;
                        res.fileLenBinary = 0;
                        getaasxFile_fileTransmitted = 0;
                    }
                }

                if (tdPending.Count != 0)
                {
                    foreach (var tdp in tdPending)
                    {
                        tf.data.Add(tdp);
                    }

                    tdPending.Clear();
                }

                var envi = 0;
                while (env[envi] != null)
                {
                    foreach (var sm in env[envi].AasEnv.Submodels)
                    {
                        if (sm is not {IdShort: not null})
                        {
                            continue;
                        }

                        var toPublish = submodelsToPublish.Contains(sm);
                        if (!toPublish)
                        {
                            var count = sm.Qualifiers.Count;
                            if (count != 0)
                            {
                                var j = 0;

                                while (j < count) // Scan qualifiers
                                {
                                    var p = sm.Qualifiers[j] as Qualifier;

                                    if (p.Type == "PUBLISH")
                                    {
                                        toPublish = true;
                                    }

                                    j++;
                                }
                            }
                        }

                        if (!toPublish)
                        {
                            continue;
                        }

                        td = new TransmitData
                        {
                            source = connectNodeName
                        };

                        var json = JsonConvert.SerializeObject(sm, Formatting.Indented);
                        td.type = "submodel";
                        td.publish.Add(json);
                        tf.data.Add(td);
                        Console.WriteLine($"Publish Submodel {sm.IdShort}");
                    }

                    envi++;
                }

                // i40language
                if (i40LanguageRuntime.isRequester && i40LanguageRuntime.sendFrameJSONRequester.Count != 0)
                {
                    foreach (var s in i40LanguageRuntime.sendFrameJSONRequester)
                    {
                        td = new TransmitData
                        {
                            source = connectNodeName,
                            type = "i40LanguageRuntime.sendFrameJSONRequester"
                        };

                        var json = JsonConvert.SerializeObject(s, Formatting.Indented);
                        td.publish.Add(json);
                        tf.data.Add(td);
                    }

                    i40LanguageRuntime.sendFrameJSONRequester.Clear();
                }

                if (i40LanguageRuntime.isProvider && i40LanguageRuntime.sendFrameJSONProvider.Count != 0)
                {
                    td = new TransmitData
                    {
                        source = connectNodeName
                    };

                    foreach (var s in i40LanguageRuntime.sendFrameJSONProvider)
                    {
                        td.type = "i40LanguageRuntime.sendFrameJSONProvider";
                        var json = JsonConvert.SerializeObject(s, Formatting.Indented);
                        td.publish.Add(json);
                        tf.data.Add(td);
                    }

                    i40LanguageRuntime.sendFrameJSONProvider.Clear();
                }

                var publish = JsonConvert.SerializeObject(tf, Formatting.Indented);

                HttpClient httpClient;
                httpClient = clientHandler != null ? new HttpClient(clientHandler) : new HttpClient();

                var contentJson = new StringContent(publish, Encoding.UTF8, "application/json");

                var content = string.Empty;
                try
                {
                    var result = httpClient.PostAsync($"{connectServer}/publish", contentJson).Result;
                    content = ContentToString(result.Content);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var newConnectData = false;

                    try
                    {
                        var tf2 = JsonConvert.DeserializeObject<TransmitFrame>(content);

                        foreach (var td2 in tf2.data)
                        {
                            switch (td2.type)
                            {
                                case "getDirectory":
                                    Console.WriteLine("received getDirectory");
                                    getDirectory = true;
                                    getDirectoryDestination = td2.source;
                                    break;
                                case "getaasx" when td2.destination == connectNodeName:
                                {
                                    var aasIndex = Convert.ToInt32(td2.extensions);

                                    dynamic res = new ExpandoObject();

                                    var binaryFile = File.ReadAllBytes(envFileName[aasIndex]);
                                    var binaryBase64 = Convert.ToBase64String(binaryFile);

                                    var payload = $"{{ \"file\" : \" {binaryBase64} \" }}";

                                    var enc = new ASCIIEncoding();
                                    var fileToken = JWT.Encode(payload, enc.GetBytes(AasxHttpContextHelper.secretString), JwsAlgorithm.HS256);

                                    if (fileToken.Length <= blockSize)
                                    {
                                        res.fileName = Path.GetFileName(envFileName[aasIndex]);
                                        res.fileData = fileToken;

                                        string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                                        var tdp = new TransmitData
                                        {
                                            source = connectNodeName,
                                            destination = td2.source,
                                            type = "getaasxFile"
                                        };

                                        tdp.publish.Add(responseJson);
                                        tdPending.Add(tdp);
                                    }
                                    else
                                    {
                                        getaasxFile_destination = td2.source;
                                        getaasxFile_fileName = Path.GetFileName(envFileName[aasIndex]);
                                        getaasxFile_fileData = fileToken;
                                        getaasxFile_fileType = "getaasxFileStream";
                                        getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                        getaasxFile_fileLenBinary = binaryFile.Length;
                                        getaasxFile_fileTransmitted = 0;
                                    }

                                    break;
                                }
                                case "getaasxstream" when td2.destination == connectNodeName:
                                {
                                    var aasIndex = Convert.ToInt32(td2.extensions);

                                    dynamic res = new ExpandoObject();

                                    var binaryFile = File.ReadAllBytes(envFileName[aasIndex]);
                                    var binaryBase64 = Convert.ToBase64String(binaryFile);

                                    if (binaryBase64.Length <= blockSize)
                                    {
                                        res.fileName = Path.GetFileName(envFileName[aasIndex]);
                                        res.fileData = binaryBase64;

                                        string responseJson = JsonConvert.SerializeObject(res, Formatting.Indented);

                                        var tdp = new TransmitData
                                        {
                                            source = connectNodeName,
                                            destination = td2.source,
                                            type = "getaasxFile"
                                        };

                                        tdp.publish.Add(responseJson);
                                        tdPending.Add(tdp);
                                    }
                                    else
                                    {
                                        getaasxFile_destination = td2.source;
                                        getaasxFile_fileName = Path.GetFileName(envFileName[aasIndex]);
                                        getaasxFile_fileData = binaryBase64;
                                        getaasxFile_fileType = "getaasxFile";
                                        getaasxFile_fileLenBase64 = getaasxFile_fileData.Length;
                                        getaasxFile_fileLenBinary = binaryFile.Length;
                                        getaasxFile_fileTransmitted = 0;
                                    }

                                    break;
                                }
                            }

                            if (td2.type.ToLower().Contains("timeseries"))
                            {
                                var split = td2.type.Split('.');
                                foreach (var smc in TimeSeries.timeSeriesSubscribe)
                                {
                                    if (smc.IdShort != split[0])
                                    {
                                        continue;
                                    }

                                    foreach (var tsb in TimeSeries.timeSeriesBlockList)
                                    {
                                        if (tsb.sampleStatus.Value == "stop")
                                        {
                                            tsb.sampleStatus.Value = "stopped";
                                        }

                                        if (tsb.sampleStatus.Value != "start")
                                            continue;

                                        if (tsb.block != smc) continue;
                                        foreach (var data in td2.publish)
                                        {
                                            using TextReader reader = new StringReader(data);
                                            var serializer = new JsonSerializer();
                                            serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            var smcData = (SubmodelElementCollection) serializer.Deserialize(reader,
                                                typeof(SubmodelElementCollection));
                                            if (smcData == null || smc.Value.Count >= 100)
                                            {
                                                continue;
                                            }

                                            if (tsb.data == null)
                                            {
                                                continue;
                                            }

                                            var maxCollections = Convert.ToInt32(tsb.maxCollections.Value);
                                            var actualCollections = tsb.data.Value.Count;
                                            if (actualCollections < maxCollections ||
                                                (tsb.sampleMode.Value == "continuous" && actualCollections == maxCollections))
                                            {
                                                tsb.data.Value.Add(smcData);
                                                actualCollections++;
                                            }

                                            if (actualCollections > maxCollections)
                                            {
                                                tsb.data.Value.RemoveAt(0);
                                                actualCollections--;
                                            }

                                            tsb.actualCollections.Value = actualCollections.ToString();
                                            SignalNewData(1);
                                        }
                                    }
                                }
                            }

                            if (td2.type.Equals("submodel"))
                            {
                                foreach (var sm in td2.publish)
                                {
                                    Submodel submodel;
                                    try
                                    {
                                        using TextReader reader = new StringReader(sm);
                                        var serializer = new JsonSerializer();
                                        serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                        submodel = (Submodel) serializer.Deserialize(reader, typeof(Submodel));
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Can not read SubModel!");
                                        return;
                                    }

                                    // need id for idempotent behaviour
                                    if (submodel.Id == null)
                                    {
                                        Console.WriteLine("Identification of SubModel is (null)!");
                                        return;
                                    }

                                    IAssetAdministrationShell aas = null;
                                    envi = 0;
                                    while (env[envi] != null)
                                    {
                                        aas = env[envi].AasEnv.FindAasWithSubmodelId(submodel.Id);
                                        if (aas != null)
                                            break;
                                        envi++;
                                    }


                                    if (aas == null)
                                    {
                                        continue;
                                    }

                                    // datastructure update
                                    if (env?[envi].AasEnv == null)
                                    {
                                        Console.WriteLine("Error accessing internal data structures.");
                                        return;
                                    }

                                    var existingSm = env[envi].AasEnv.FindSubmodelById(submodel.Id);
                                    if (existingSm == null)
                                    {
                                        continue;
                                    }

                                    var toSubscribe = submodelsToSubscribe.Contains(existingSm);
                                    if (!toSubscribe)
                                    {
                                        var eqcount = existingSm.Qualifiers.Count;
                                        if (eqcount != 0)
                                        {
                                            var j = 0;

                                            while (j < eqcount) // Scan qualifiers
                                            {
                                                var p = existingSm.Qualifiers[j] as Qualifier;

                                                if (p.Type == "SUBSCRIBE")
                                                {
                                                    toSubscribe = true;
                                                    break;
                                                }

                                                j++;
                                            }
                                        }
                                    }

                                    if (!toSubscribe)
                                    {
                                        continue;
                                    }

                                    Console.WriteLine($"Subscribe Submodel {submodel.IdShort}");

                                    var c2 = submodel.Qualifiers.Count;
                                    if (c2 != 0)
                                    {
                                        var k = 0;

                                        while (k < c2) // Scan qualifiers
                                        {
                                            var q = submodel.Qualifiers[k] as Qualifier;

                                            if (q.Type == "PUBLISH")
                                            {
                                                q.Type = "SUBSCRIBE";
                                            }

                                            k++;
                                        }
                                    }

                                    var overwrite = true;
                                    var escount = existingSm.SubmodelElements.Count;
                                    var count2 = submodel.SubmodelElements.Count;
                                    if (escount == count2)
                                    {
                                        var smi = 0;
                                        while (smi < escount)
                                        {
                                            var sme1 = submodel.SubmodelElements[smi];
                                            var sme2 = existingSm.SubmodelElements[smi];

                                            if (sme1 is Property property1)
                                            {
                                                if (sme2 is Property property2)
                                                {
                                                    property2.Value = property1.Value;
                                                }
                                                else
                                                {
                                                    overwrite = false;
                                                    break;
                                                }
                                            }

                                            smi++;
                                        }
                                    }

                                    if (!overwrite)
                                    {
                                        env[envi].AasEnv.Submodels.Remove(existingSm);
                                        env[envi].AasEnv.Submodels.Add(submodel);

                                        // add SubmodelRef to AAS            
                                        // access the AAS
                                        var key = new Key(KeyTypes.Submodel, submodel.Id);
                                        var keyList = new List<IKey> {key};
                                        var newsmr = new Reference(ReferenceTypes.ModelReference, keyList);
                                        var existsmr = aas.HasSubmodelReference(newsmr);
                                        if (!existsmr)
                                        {
                                            aas.Submodels.Add(newsmr);
                                        }
                                    }

                                    newConnectData = true;
                                }
                            }

                            // i40language
                            if (i40LanguageRuntime.isRequester && td2.type.Equals("i40LanguageRuntime.sendFrameJSONProvider"))
                            {
                                foreach (var s in td2.publish)
                                {
                                    i40LanguageRuntime.receivedFrameJSONRequester.Add(JsonConvert.DeserializeObject<string>(s));
                                }
                            }

                            if (!i40LanguageRuntime.isProvider || td2.type != "i40LanguageRuntime.sendFrameJSONRequester")
                            {
                                continue;
                            }

                            foreach (var s in td2.publish)
                            {
                                i40LanguageRuntime.receivedFrameJSONProvider.Add(JsonConvert.DeserializeObject<string>(s));
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }

                    if (newConnectData)
                    {
                        NewDataAvailable?.Invoke(null, EventArgs.Empty);
                    }
                }

                Thread.Sleep(!string.IsNullOrEmpty(getaasxFile_destination)
                    ? 500
                    : connectUpdateRate); // block transfer
            }
        }

        private static Timer OPCClientTimer;
        static bool timerSet;

        private static void SetOPCClientTimer(double value)
        {
            if (timerSet)
            {
                return;
            }

            timerSet = true;
            TimeSeries.SetOPCClientThread(value);
        }

        public static event EventHandler NewDataAvailable;

        public class NewDataAvailableEventArgs : EventArgs
        {
            public int signalNewDataMode;

            public NewDataAvailableEventArgs(int mode = 2)
            {
                signalNewDataMode = mode;
            }
        }

        // 0 == same tree, only values changed
        // 1 == same tree, structure may change
        // 2 == build new tree, keep open nodes
        // 3 == build new tree, all nodes closed
        public static void SignalNewData(int mode)
        {
            NewDataAvailable?.Invoke(null, new NewDataAvailableEventArgs(mode));
        }

        public static void OnOPCClientNextTimedEvent()
        {
            ReadOPCClient(false);
            NewDataAvailable?.Invoke(null, EventArgs.Empty);
        }

        private static Timer _scriptTimer;

        private static void SetScriptTimer(double value)
        {
            // Create a timer with a two-second interval.
            _scriptTimer = new Timer(value);
            // Hook up the Elapsed event for the timer. 
            _scriptTimer.Elapsed += OnScriptTimedEvent;
            _scriptTimer.AutoReset = true;
            _scriptTimer.Enabled = true;
        }

        private static void OnScriptTimedEvent(object source, ElapsedEventArgs e)
        {
            RunScript(false);
        }

        private static Timer restTimer;

        static bool RESTalreadyRunning;
        static long countGetPut;

        /// <summary>
        /// Writes to (i.e. updates values of) Nodes in the AAS OPC Server
        /// </summary>
        private static bool OPCWrite(string nodeId, object value)
        {
            if (!runOPC)
            {
                return true;
            }

            var nodeMgr = AasEntityBuilder.nodeMgr;

            if (nodeMgr == null)
            {
                // if Server has not started yet, the AasNodeManager is null
                Console.WriteLine("OPC NodeManager not initialized.");
                return false;
            }

            // Find node in Core3OPC Server to update it

            if (nodeMgr.Find(nodeId) is not BaseVariableState bvs)
            {
                Console.WriteLine("node {0} does not exist in server!", nodeId);
                return false;
            }

            var convertedValue = Convert.ChangeType(value, bvs.Value.GetType());
            if (Equals(bvs.Value, convertedValue))
            {
                return true;
            }

            bvs.Value = convertedValue;
            // TODO: timestamp UtcNow okay or get this internally from the Server?
            bvs.Timestamp = DateTime.UtcNow;
            bvs.ClearChangeMasks(null, false);

            return true;
        }

        /// <summary>
        /// Update AAS property values from external OPC servers.
        /// Only submodels which have the appropriate qualifier are affected.
        /// However, this will attempt to get values for all properties of the submodel.
        /// TODO: Possilby add a qualifier to specifiy which values to get? Or NodeIds per alue?
        /// </summary>
        static bool ReadOPCClient(bool initial)
        {
            if (env == null)
                return false;

            lock (changeAasxFile)
            {
                var i = 0;
                while (env[i] != null)
                {
                    foreach (var sm in env[i].AasEnv.Submodels)
                    {
                        var count = sm.Qualifiers.Count;
                        if (sm is not {IdShort: not null} || count == 0)
                        {
                            continue;
                        }

                        const int stopTimeout = Timeout.Infinite;
                        const bool autoAccept = true;
                        // Variablen aus AAS Qualifiern
                        var Username = string.Empty;
                        var Password = string.Empty;
                        var URL = string.Empty;
                        var Namespace = 0;
                        var Path = string.Empty;

                        var j = 0;

                        while (j < count) // URL, Username, Password, Namespace, Path
                        {
                            var p = sm.Qualifiers[j] as Qualifier;

                            switch (p.Type)
                            {
                                case "OPCURL": // URL
                                    URL = p.Value;
                                    break;
                                case "OPCUsername": // Username
                                    Username = p.Value;
                                    break;
                                case "OPCPassword": // Password
                                    Password = p.Value;
                                    break;
                                case "OPCNamespace": // Namespace
                                    // TODO: if not int, currently throws nondescriptive error
                                    if (int.TryParse(p.Value, out var tmpI))
                                        Namespace = tmpI;
                                    break;
                                case "OPCPath": // Path
                                    Path = p.Value;
                                    break;
                                case "OPCEnvVar": // Only if environment variable ist set
                                    // VARIABLE=VALUE
                                    var split = p.Value.Split('=');
                                    if (split.Length == 2 && envVariables.TryGetValue(split[0], out var value))
                                    {
                                        if (split[1] != value)
                                        {
                                            URL = string.Empty;
                                        }
                                    }

                                    break;
                            }

                            j++;
                        }

                        if (string.IsNullOrEmpty(URL))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(URL) ||
                            Namespace == 0 ||
                            string.IsNullOrEmpty(Path) ||
                            (string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password)) ||
                            (!string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password)))
                        {
                            Console.WriteLine("Incorrect or missing qualifier. Aborting ...");
                            return false;
                        }


                        if (string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password))
                        {
                            Console.WriteLine("Using Anonymous to login ...");
                        }

                        // try to get the client from dictionary, else create and add it
                        UASampleClient client;
                        lock (opcclientAddLock)
                        {
                            if (!OPCClients.TryGetValue(URL, out client))
                            {
                                var cantconnect = false;
                                try
                                {
                                    // make OPC UA client
                                    client = new UASampleClient(URL, autoAccept, stopTimeout, Username, Password);
                                    Console.WriteLine("Connecting to external OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                                    client.ConsoleSampleClient().Wait();
                                    // add it to the dictionary under these submodels idShort
                                    OPCClients.Add(URL, client);
                                }
                                catch (AggregateException ae)
                                {
                                    ae.Handle(x =>
                                        {
                                            if (x is not ServiceResultException)
                                            {
                                                return false; // others not handled, will cause unhandled exception
                                            }

                                            cantconnect = true;
                                            return true; // this exception handled
                                        }
                                    );
                                    if (cantconnect)
                                    {
                                        // stop processing OPC read because we couldn't connect
                                        // but return true as this shouldn't stop the main loop
                                        Console.WriteLine(ae.Message);
                                        Console.WriteLine("Could not connect to {0} with {1} ...", URL, sm.IdShort);
                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Already connected to OPC UA Server at {0} with {1} ...", URL, sm.IdShort);
                            }
                        }

                        Console.WriteLine("==================================================");
                        Console.WriteLine("Read values for {0} from {1} ...", sm.IdShort, URL);
                        Console.WriteLine("==================================================");

                        // over all SMEs
                        count = sm.SubmodelElements.Count;
                        for (j = 0; j < count; j++)
                        {
                            var sme = sm.SubmodelElements[j];
                            // some preparations for multiple AAS below
                            const int serverNamespaceIdx = 3; //could be gotten directly from the nodeMgr in OPCWrite instead, only pass the string part of the ID

                            var aasSubmodel = $"{env[i].AasEnv.AssetAdministrationShells[0].IdShort}.{sm.IdShort}";
                            var serverNodePrefix = $"ns={serverNamespaceIdx};s=AASROOT.{aasSubmodel}";
                            WalkSubmodelElement(sme, Path, serverNodePrefix, client, Namespace);
                        }
                    }

                    i++;
                }
            }

            if (!initial)
            {
                ChangeDataVersion();
            }

            return true;
        }

        static void RunScript(bool init)
        {
            if (env == null)
                return;

            lock (changeAasxFile)
            {
                var i = 0;
                while (i < env.Length && env[i] != null)
                {
                    if (env[i].AasEnv.Submodels != null)
                    {
                        foreach (var sm in env[i].AasEnv.Submodels)
                        {
                            if (sm is not {IdShort: not null})
                            {
                                continue;
                            }

                            var count = sm.Qualifiers?.Count ?? 0;
                            if (count == 0)
                            {
                                continue;
                            }

                            var q = sm.Qualifiers[0] as Qualifier;
                            if (q.Type != "SCRIPT")
                            {
                                continue;
                            }

                            // Triple
                            // Reference to property with Number
                            // Reference to submodel with numbers/strings
                            // Reference to property to store found text
                            count = sm.SubmodelElements.Count;
                            var smi = 0;
                            while (smi < count)
                            {
                                var sme1 = sm.SubmodelElements[smi++];
                                if (sme1.Qualifiers == null || sme1.Qualifiers.Count == 0)
                                {
                                    continue;
                                }

                                var qq = sme1.Qualifiers[0] as Qualifier;

                                switch (qq.Type)
                                {
                                    case "Add":
                                    {
                                        var v = Convert.ToInt32((sme1 as Property).Value);
                                        v += Convert.ToInt32(qq.Value);
                                        (sme1 as Property).Value = v.ToString();
                                        continue;
                                    }
                                    case "GetJSON" when init:
                                    case "GetJSON" when isLoading:
                                        return;
                                    case "GetJSON" when sme1 is not ReferenceElement:
                                        continue;
                                    case "GetJSON":
                                    {
                                        var url = qq.Value;
                                        var username = string.Empty;
                                        var password = string.Empty;

                                        if (sme1.Qualifiers.Count == 3)
                                        {
                                            qq = sme1.Qualifiers[1] as Qualifier;
                                            if (qq.Type != "Username")
                                                continue;
                                            username = qq.Value;
                                            qq = sme1.Qualifiers[2] as Qualifier;
                                            if (qq.Type != "Password")
                                                continue;
                                            password = qq.Value;
                                        }

                                        var handler = new HttpClientHandler();
                                        handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                                        var client = new HttpClient(handler);

                                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                                        {
                                            var authToken = Encoding.ASCII.GetBytes($"{username}:{password}");
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                Convert.ToBase64String(authToken));
                                        }

                                        Console.WriteLine($"GetJSON: {url}");
                                        var response = client.GetStringAsync(url).Result;
                                        Console.WriteLine(response);

                                        if (!string.IsNullOrEmpty(response))
                                        {
                                            var r12 = sme1 as ReferenceElement;
                                            var ref12 = env[i].AasEnv.FindReferableByReference(r12.GetModelReference());
                                            if (ref12 is SubmodelElementCollection collection)
                                            {
                                                var parsed = JObject.Parse(response);
                                                ParseJson(collection, parsed, null);
                                            }
                                        }

                                        continue;
                                    }
                                }

                                if (qq.Type != "SearchNumber" || smi >= count)
                                {
                                    continue;
                                }

                                var sme2 = sm.SubmodelElements[smi++];
                                if (sme2.Qualifiers.Count == 0)
                                {
                                    continue;
                                }

                                qq = sme2.Qualifiers[0] as Qualifier;
                                if (qq.Type != "SearchList" || smi >= count)
                                {
                                    continue;
                                }

                                var sme3 = sm.SubmodelElements[smi++];
                                if (sme3.Qualifiers.Count == 0)
                                {
                                    continue;
                                }

                                qq = sme3.Qualifiers[0] as Qualifier;
                                if (qq.Type != "SearchResult")
                                {
                                    break;
                                }

                                if (sme1 is not ReferenceElement referenceElement1 ||
                                    sme2 is not ReferenceElement referenceElement2 ||
                                    sme3 is not ReferenceElement referenceElement3)
                                {
                                    continue;
                                }

                                var ref1 = env[i].AasEnv.FindReferableByReference(referenceElement1.GetModelReference());
                                var ref2 = env[i].AasEnv.FindReferableByReference(referenceElement2.GetModelReference());
                                var ref3 = env[i].AasEnv.FindReferableByReference(referenceElement3.GetModelReference());
                                if (ref1 is not Property p1 || ref2 is not Submodel sm2 || ref3 is not Property p3)
                                {
                                    continue;
                                }

                                // Simulate changes
                                var count2 = sm2.SubmodelElements.Count;
                                for (var j = 0; j < count2; j++)
                                {
                                    var sme = sm2.SubmodelElements[j];
                                    if (sme.IdShort == p1.Value)
                                    {
                                        p3.Value = (sme as Property).Value;
                                    }
                                }
                            }
                        }
                    }

                    i++;
                }
            }
        }

        public static bool ParseJson(SubmodelElementCollection c, JObject o, List<string> filter,
            Property minDiffAbsolute = null, Property minDiffPercent = null)
        {
            var newMode = 0;
            var timeStamp = DateTime.UtcNow;
            var ok = false;

            var iMinDiffAbsolute = 1;
            var iMinDiffPercent = 0;
            if (minDiffAbsolute != null)
                iMinDiffAbsolute = Convert.ToInt32(minDiffAbsolute.Value);
            if (minDiffPercent != null)
                iMinDiffPercent = Convert.ToInt32(minDiffPercent.Value);

            foreach (JProperty jp1 in (JToken) o)
            {
                if (filter != null && filter.Count != 0 && !filter.Contains(jp1.Name))
                {
                    continue;
                }

                SubmodelElementCollection c2;
                switch (jp1.Value.Type)
                {
                    case JTokenType.Array:
                        c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            c2 = new SubmodelElementCollection(idShort: jp1.Name);
                            c.Value.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }

                        var count = 1;
                        foreach (JObject el in jp1.Value)
                        {
                            var n = $"{jp1.Name}_array_{count++}";
                            var c3 =
                                c2.FindFirstIdShortAs<SubmodelElementCollection>(n);
                            if (c3 == null)
                            {
                                c3 = new SubmodelElementCollection(idShort: n);
                                c2.Value.Add(c3);
                                c3.TimeStampCreate = timeStamp;
                                c3.SetTimeStamp(timeStamp);
                                newMode = 1;
                            }

                            ok |= ParseJson(c3, el, filter);
                        }

                        break;
                    case JTokenType.Object:
                        c2 = c.FindFirstIdShortAs<SubmodelElementCollection>(jp1.Name);
                        if (c2 == null)
                        {
                            c2 = new SubmodelElementCollection(idShort: jp1.Name);
                            c.Value.Add(c2);
                            c2.TimeStampCreate = timeStamp;
                            c2.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }

                        ok = jp1.Value.Cast<JObject>().Aggregate(ok, (current, el) => current | ParseJson(c2, el, filter));

                        break;
                    default:
                        var p = c.FindFirstIdShortAs<Property>(jp1.Name);
                        if (p == null)
                        {
                            p = new Property(DataTypeDefXsd.String, idShort: jp1.Name);
                            c.Value.Add(p);
                            p.TimeStampCreate = timeStamp;
                            p.SetTimeStamp(timeStamp);
                            newMode = 1;
                        }

                        // see https://github.com/JamesNK/Newtonsoft.Json/issues/874    
                        try
                        {
                            if (string.IsNullOrEmpty(p.Value))
                                p.Value = "0";
                            var value = (jp1.Value as JValue).ToString(CultureInfo.InvariantCulture);
                            if (!value.Contains('.'))
                            {
                                var v = Convert.ToInt32(value);
                                var lastv = Convert.ToInt32(p.Value);
                                var delta = Math.Abs(v - lastv);
                                if (delta >= iMinDiffAbsolute && delta >= lastv * iMinDiffPercent / 100)
                                {
                                    p.Value = value;
                                    p.SetTimeStamp(timeStamp);
                                    ok = true;
                                }
                            }
                            else
                            {
                                p.Value = value;
                                p.SetTimeStamp(timeStamp);
                                ok = true;
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception.Message);
                        }

                        break;
                }
            }

            SignalNewData(newMode);
            return ok;
        }

        private static void WalkSubmodelElement(IClass sme, string nodePath, string serverNodePrefix, UASampleClient client, int clientNamespace)
        {
            switch (sme)
            {
                case Property property:
                {
                    var clientNodeName = nodePath + property.IdShort;
                    var serverNodeId = $"{serverNodePrefix}.{property.IdShort}.Value";
                    var clientNode = new NodeId(clientNodeName, (ushort) clientNamespace);
                    UpdatePropertyFromOPCClient(property, serverNodeId, client, clientNode);
                    break;
                }
                case SubmodelElementCollection elementCollection:
                {
                    foreach (var t in elementCollection.Value)
                    {
                        var newNodeIdBase = $"{nodePath}.{elementCollection.IdShort}";
                        WalkSubmodelElement(t, newNodeIdBase, serverNodePrefix, client, clientNamespace);
                    }

                    break;
                }
            }
        }

        private static void UpdatePropertyFromOPCClient(IProperty p, string serverNodeId, UASampleClient client, NodeId clientNodeId)
        {
            var value = string.Empty;

            var write = (p.FindQualifierOfType("OPCWRITE") != null);
            if (write)
                value = p.Value;

            try
            {
                var split = (clientNodeId.ToString()).Split('#');
                if (split.Length == 2)
                {
                    uint i = Convert.ToUInt16(split[1]);
                    split = clientNodeId.ToString().Split('=');
                    split = split[1].Split(';');
                    var ns = Convert.ToUInt16(split[0]);
                    clientNodeId = new NodeId(i, ns);
                    Console.WriteLine($"New node id: {clientNodeId}");
                }

                Console.WriteLine($"{serverNodeId} <= {value}");
                if (write)
                {
                    var i = Convert.ToInt16(value);
                    client.WriteSubmodelElementValue(clientNodeId, i);
                }
                else
                    value = client.ReadSubmodelElementValue(clientNodeId);
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine($"OPC ServiceResultException ({ex.Message}) trying to read {clientNodeId.ToString()}");
                return;
            }

            // update in AAS env
            if (write)
            {
                return;
            }

            p.Value = value;
            SignalNewData(0);

            // update in OPC
            if (!OPCWrite(serverNodeId, value))
            {
                Console.WriteLine("OPC write not successful.");
            }
        }
    }

    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask;

        public override void Message(string text, bool ask)
        {
            message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }

            if (!ask)
            {
                return await Task.FromResult(true);
            }

            try
            {
                var result = Console.ReadKey();
                Console.WriteLine();
                return await Task.FromResult(result.KeyChar is 'y' or 'Y' or '\r');
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            return await Task.FromResult(true);
        }
    }

    public enum ExitCode
    {
        Ok = 0,
        ErrorServerNotStarted = 0x80,
        ErrorServerRunning = 0x81,
        ErrorServerException = 0x82
    }

    public class MySampleServer
    {
        SampleServer server;
        Task status;
        DateTime lastEventTime;
        int serverRunTime = Timeout.Infinite;
        static bool autoAccept;
        static ExitCode exitCode;

        static AdminShellPackageEnv[] aasxEnv;

        // OZ
        public static ManualResetEvent quitEvent;

        public MySampleServer(bool _autoAccept, int _stopTimeout, AdminShellPackageEnv[] _aasxEnv)
        {
            autoAccept = _autoAccept;
            aasxEnv = _aasxEnv;
            serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
        }

        public void Run()
        {
            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                ConsoleSampleServer().Wait();
                Console.WriteLine("Servers successfully started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorServerRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorServerException;
                return;
            }

            quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (_, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(serverRunTime);

            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");

                using var sampleServer = server;
                // Stop status thread
                server = null;
                status.Wait();
                // Stop server and dispose
                sampleServer.Stop();
            }

            exitCode = ExitCode.Ok;
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
            }
        }

        private async Task ConsoleSampleServer()
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            var application = new ApplicationInstance
            {
                ApplicationName = "UA Core Sample Server",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleServer" : "Opc.Ua.SampleServer"
            };

            // load the application configuration.
            var config = await application.LoadApplicationConfiguration(true);

            // check the application certificate.
            var haveAppCertificate = await application.CheckApplicationInstanceCertificate(true, 0);

            if (!haveAppCertificate)
            {
                throw new AuthenticationException("Application instance certificate invalid!");
            }

            if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
            }

            // start the server.
            server = new SampleServer(aasxEnv);
            await application.Start(server);

            // start the status thread
            status = Task.Run(StatusThread);

            // print notification on session events
            server.CurrentInstance.SessionManager.SessionActivated += EventStatus;
            server.CurrentInstance.SessionManager.SessionClosing += EventStatus;
            server.CurrentInstance.SessionManager.SessionCreated += EventStatus;
        }

        private void EventStatus(Session session, SessionEventReason reason)
        {
            lastEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        private static void PrintSessionStatus(Session session, string reason, bool lastContact = false)
        {
            lock (session.DiagnosticsLock)
            {
                var item = $"{reason,9}:{session.SessionDiagnostics.SessionName,20}:";
                if (lastContact)
                {
                    item += $"Last Event:{session.SessionDiagnostics.ClientLastContactTime.ToLocalTime():HH:mm:ss}";
                }
                else
                {
                    if (session.Identity != null)
                    {
                        item += $":{session.Identity.DisplayName,20}";
                    }

                    item += $":{session.Id}";
                }

                Console.WriteLine(item);
            }
        }

        private async void StatusThread()
        {
            while (server != null)
            {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000))
                {
                    var sessions = server.CurrentInstance.SessionManager.GetSessions();
                    foreach (var session in sessions)
                    {
                        PrintSessionStatus(session, "-Status-", true);
                    }

                    lastEventTime = DateTime.UtcNow;
                }

                await Task.Delay(1000);
            }
        }
    }
}