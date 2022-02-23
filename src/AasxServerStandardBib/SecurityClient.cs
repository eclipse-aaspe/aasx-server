﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AasxRestServerLibrary;
using AasxServer;
using AdminShellNS;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AasxServer
{
    public class AasxTask
    {
        public AdminShell.SubmodelElementCollection def = null;
        public AdminShell.Property taskType = null;
        public AdminShell.Property cycleTime = null;
        public AdminShell.Property cycleCount = null;
        public AdminShell.Property nextCycle = null;
        public DateTime nextExecution = new DateTime();
        public int envIndex = -1;

        public static List<AasxTask> taskList = null;

        public static WebProxy proxy = null;

        public static void taskInit()
        {
            // Test for proxy
            Console.WriteLine("Test: ../proxy.dat");

            if (File.Exists("../proxy.dat"))
            {
                bool error = false;
                Console.WriteLine("Found: ../proxy.dat");
                string proxyAddress = "";
                string username = "";
                string password = "";
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader("../proxy.dat"))
                    {
                        proxyAddress = sr.ReadLine();
                        username = sr.ReadLine();
                        password = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("The file ../proxy.dat could not be read:");
                    Console.WriteLine(e.Message);
                    error = true;
                }
                if (!error)
                {
                    Console.WriteLine("Proxy: " + proxyAddress);
                    Console.WriteLine("Username: " + username);
                    Console.WriteLine("Password: " + password);
                    proxy = new WebProxy();
                    Uri newUri = new Uri(proxyAddress);
                    proxy.Address = newUri;
                    if (username != "" && password != "")
                        proxy.Credentials = new NetworkCredential(username, password);
                }
            }


            DateTime timeStamp = DateTime.UtcNow;
            taskList = new List<AasxTask>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null && sm.idShort.ToLower().Contains("tasks"))
                            {
                                sm.setTimeStamp(timeStamp);
                                int countSme = sm.submodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.submodelElements[iSme].submodelElement;
                                    if (sme is AdminShell.SubmodelElementCollection smec)
                                    {
                                        var nextTask = new AasxTask();
                                        AasxTask.taskList.Add(nextTask);
                                        nextTask.def = smec;
                                        nextTask.envIndex = i;

                                        int countSmec = smec.value.Count;
                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.value[iSmec].submodelElement;
                                            var idShort = sme2.idShort.ToLower();

                                            switch (idShort)
                                            {
                                                case "tasktype":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.taskType = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "cycletime":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.cycleTime = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "cyclecount":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.cycleCount = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "nextcycle":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        nextTask.nextCycle = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                            }
                                        }

                                        if (nextTask.taskType?.value.ToLower() == "init")
                                            runOperations(nextTask.def, nextTask.envIndex, timeStamp);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tasksThread = new Thread(new ThreadStart(tasksSamplingLoop));
            // MICHA
            tasksThread.Start();
        }

        static void runOperations(AdminShell.SubmodelElementCollection smec, int envIndex, DateTime timeStamp)
        {
            int countSmec = smec.value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.value[iSmec].submodelElement;

                if (sme2 is AdminShell.Operation op)
                {
                    var idShort = sme2.idShort.ToLower();
                    switch (idShort)
                    {
                        case "authenticate":
                            operation_authenticate(op, envIndex, timeStamp);
                            break;
                        case "get":
                        case "getdiff":
                        case "put":
                        case "putdiff":
                            operation_get_put(op, envIndex, timeStamp);
                            break;
                        case "limitcount":
                            operation_limitCount(op, envIndex, timeStamp);
                            break;
                        case "calculatecfp":
                        case "calculate_cfp":
                            operation_calculate_cfp(op, envIndex, timeStamp);
                            break;
                    }
                }
            }
        }

        static void operation_authenticate(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection

            if (op.inputVariable.Count != 1)
            {
                return;
            }

            AdminShell.Property authType = null;
            AdminShell.Property authServerEndPoint = null;
            AdminShell.File authServerCertificate = null;
            AdminShell.File clientCertificate = null;
            AdminShell.Property clientCertificatePassWord = null;
            AdminShell.Property accessToken = null;
            AdminShell.Property userName = null;
            AdminShell.Property passWord = null;

            var smec = new AdminShell.SubmodelElementCollection();
            foreach (var input in op.inputVariable)
            {
                var inputRef = input.value.submodelElement;
                if (!(inputRef is AdminShell.ReferenceElement))
                    return;
                var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                if (refElement is AdminShell.SubmodelElementCollection re)
                    smec = re;
            }

            int countSmec = smec.value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.value[iSmec].submodelElement;
                var idShort = sme2.idShort.ToLower();

                switch (idShort)
                {
                    case "authtype":
                        if (sme2 is AdminShell.Property)
                        {
                            authType = sme2 as AdminShell.Property;
                        }
                        break;
                    case "authserverendpoint":
                        if (sme2 is AdminShell.Property)
                        {
                            authServerEndPoint = sme2 as AdminShell.Property;
                        }
                        break;
                    case "accesstoken":
                        if (sme2 is AdminShell.Property)
                        {
                            accessToken = sme2 as AdminShell.Property;
                        }
                        break;
                    case "username":
                        if (sme2 is AdminShell.Property)
                        {
                            userName = sme2 as AdminShell.Property;
                        }
                        break;
                    case "password":
                        if (sme2 is AdminShell.Property)
                        {
                            passWord = sme2 as AdminShell.Property;
                        }
                        break;
                    case "authservercertificate":
                        if (sme2 is AdminShell.File)
                        {
                            authServerCertificate = sme2 as AdminShell.File;
                        }
                        break;
                    case "clientcertificate":
                        if (sme2 is AdminShell.File)
                        {
                            clientCertificate = sme2 as AdminShell.File;
                        }
                        break;
                    case "clientcertificatepassword":
                        if (sme2 is AdminShell.Property)
                        {
                            clientCertificatePassWord = sme2 as AdminShell.Property;
                        }
                        break;
                }
            }

            if (authType != null)
            {
                switch (authType.value.ToLower())
                {
                    case "openid":
                        if (authServerEndPoint != null && authServerCertificate != null && clientCertificate != null
                            && accessToken != null)
                        {
                            if (accessToken.value != "")
                            {
                                bool valid = true;
                                var jwtToken = new JwtSecurityToken(accessToken.value);
                                if ((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow))
                                    valid = false;
                                if (valid) return;
                            }

                            var handler = new HttpClientHandler();
                            if (proxy != null)
                                handler.Proxy = proxy;
                            else
                                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                            var client = new HttpClient(handler);
                            DiscoveryDocumentResponse disco = null;

                            var task = Task.Run(async () => { disco = await client.GetDiscoveryDocumentAsync(authServerEndPoint.value); });
                            task.Wait();
                            if (disco.IsError) return;
                            Console.WriteLine("OpenID Discovery JSON:");
                            Console.WriteLine(disco.Raw);

                            var serverCert = new X509Certificate2();
                            Stream s = null;
                            try
                            {
                                s = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(authServerCertificate.value);
                            }
                            catch { }
                            if (s == null) return;

                            using (var m = new System.IO.MemoryStream())
                            {
                                s.CopyTo(m);
                                var b = m.GetBuffer();
                                serverCert = new X509Certificate2(b);
                                Console.WriteLine("Auth server certificate: " + authServerCertificate.value);
                            }

                            string[] x5c = null;
                            X509Certificate2 certificate = null;
                            string certificatePassword = clientCertificatePassWord.value;
                            Stream s2 = null;
                            try
                            {
                                s2 = AasxServer.Program.env[envIndex].GetLocalStreamFromPackage(clientCertificate.value);
                            }
                            catch { }
                            if (s2 == null) return;
                            if (s2 != null)
                            {
                                X509Certificate2Collection xc = new X509Certificate2Collection();
                                using (var m = new System.IO.MemoryStream())
                                {
                                    s2.CopyTo(m);
                                    var b = m.GetBuffer();
                                    xc.Import(b, certificatePassword, X509KeyStorageFlags.PersistKeySet);
                                    certificate = new X509Certificate2(b, certificatePassword);
                                    Console.WriteLine("Client certificate: " + clientCertificate.value);
                                }

                                string[] X509Base64 = new string[xc.Count];

                                int j = xc.Count;
                                var xce = xc.GetEnumerator();
                                for (int i = 0; i < xc.Count; i++)
                                {
                                    xce.MoveNext();
                                    X509Base64[--j] = Convert.ToBase64String(xce.Current.GetRawCertData());
                                }
                                x5c = X509Base64;

                                var credential = new X509SigningCredentials(certificate);
                                string clientId = "client.jwt";
                                string email = "";
                                string subject = certificate.Subject;
                                var split = subject.Split(new Char[] { ',' });
                                if (split[0] != "")
                                {
                                    var split2 = split[0].Split(new Char[] { '=' });
                                    if (split2[0] == "E")
                                    {
                                        email = split2[1];
                                    }
                                }
                                Console.WriteLine("email: " + email);

                                var now = DateTime.UtcNow;
                                var token = new JwtSecurityToken(
                                        clientId,
                                        disco.TokenEndpoint,
                                        new List<Claim>()
                                        {
                                                        new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                                                        new Claim(JwtClaimTypes.Subject, clientId),
                                                        new Claim(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64),
                                                        // OZ
                                                        new Claim(JwtClaimTypes.Email, email)
                                        },
                                        now,
                                        now.AddMinutes(1),
                                        credential)
                                ;

                                token.Header.Add("x5c", x5c);
                                var tokenHandler = new JwtSecurityTokenHandler();
                                string clientToken = tokenHandler.WriteToken(token);

                                TokenResponse response = null;
                                task = Task.Run(async () =>
                                {
                                    response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                                    {
                                        Address = disco.TokenEndpoint,
                                        Scope = "resource1.scope1",

                                        ClientAssertion =
                                        {
                                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                                            Value = clientToken
                                        }
                                    });
                                });
                                task.Wait();

                                if (response.IsError) return;

                                accessToken.value = response.AccessToken;
                                accessToken.setTimeStamp(timeStamp);
                            }
                        }
                        break;
                }
            }
        }

        static void operation_get_put(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference authentication: collection
            // inputVariable sourceEndPoint: property
            // inputVariable reference sourcePath: property
            // inputVariable reference destinationElement: collection

            if (op.inputVariable.Count < 3 || op.inputVariable.Count > 4)
            {
                return;
            }
            string opName = op.idShort.ToLower();

            AdminShell.SubmodelElementCollection authentication = null;
            AdminShell.Property authType = null;
            AdminShell.Property accessToken = null;
            AdminShell.Property userName = null;
            AdminShell.Property passWord = null;
            AdminShell.File authServerCertificate = null;
            AdminShell.Property endPoint = null;
            AdminShell.Property path = null;
            AdminShell.SubmodelElementCollection elementCollection = null;
            AdminShell.Submodel elementSubmodel = null;
            AdminShell.Property lastDiff = null;
            AdminShell.Property status = null;

            AdminShell.SubmodelElementCollection smec = null;
            AdminShell.Submodel sm = null;
            AdminShell.Property p = null;
            foreach (var input in op.inputVariable)
            {
                smec = null;
                sm = null;
                p = null;
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    p = (inputRef as AdminShell.Property);
                }
                if (inputRef is AdminShell.ReferenceElement)
                {
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                    if (refElement is AdminShell.SubmodelElementCollection)
                        smec = refElement as AdminShell.SubmodelElementCollection;
                    if (refElement is AdminShell.Submodel)
                        sm = refElement as AdminShell.Submodel;
                }
                switch (inputRef.idShort.ToLower())
                {
                    case "authentication":
                        if (smec != null)
                            authentication = smec;
                        break;
                    case "endpoint":
                        if (p != null)
                            endPoint = p;
                        break;
                    case "path":
                        if (p != null)
                            path = p;
                        break;
                    case "element":
                        if (smec != null)
                            elementCollection = smec;
                        if (sm != null)
                            elementSubmodel = sm;
                        break;
                }
            }
            foreach (var output in op.outputVariable)
            {
                smec = null;
                sm = null;
                p = null;
                var outputRef = output.value.submodelElement;
                if (outputRef is AdminShell.Property)
                {
                    p = (outputRef as AdminShell.Property);
                }
                if (outputRef is AdminShell.ReferenceElement)
                {
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((outputRef as AdminShell.ReferenceElement).value);
                    if (refElement is AdminShell.SubmodelElementCollection)
                        smec = refElement as AdminShell.SubmodelElementCollection;
                    if (refElement is AdminShell.Submodel)
                        sm = refElement as AdminShell.Submodel;
                }
                switch (outputRef.idShort.ToLower())
                {
                    case "lastdiff":
                        if (p != null)
                            lastDiff = p;
                        break;
                    case "status":
                        if (p != null)
                            status = p;
                        break;
                }
            }

            if (authentication != null)
            {
                smec = authentication;
                int countSmec = smec.value.Count;
                for (int iSmec = 0; iSmec < countSmec; iSmec++)
                {
                    var sme2 = smec.value[iSmec].submodelElement;
                    var idShort = sme2.idShort.ToLower();

                    switch (idShort)
                    {
                        case "authtype":
                            if (sme2 is AdminShell.Property)
                            {
                                authType = sme2 as AdminShell.Property;
                            }
                            break;
                        case "accesstoken":
                            if (sme2 is AdminShell.Property)
                            {
                                accessToken = sme2 as AdminShell.Property;
                            }
                            break;
                        case "username":
                            if (sme2 is AdminShell.Property)
                            {
                                userName = sme2 as AdminShell.Property;
                            }
                            break;
                        case "password":
                            if (sme2 is AdminShell.Property)
                            {
                                passWord = sme2 as AdminShell.Property;
                            }
                            break;
                        case "authservercertificate":
                            if (sme2 is AdminShell.File)
                            {
                                authServerCertificate = sme2 as AdminShell.File;
                            }
                            break;
                    }
                }
            }

            var handler = new HttpClientHandler();
            if (proxy != null)
                handler.Proxy = proxy;
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);
            if (accessToken != null)
                client.SetBearerToken(accessToken.value);

            string requestPath = endPoint.value + "/" + path.value;
            HttpResponseMessage response = null;
            Task task = null;
            string diffPath = "";
            var splitPath = path.value.Split('.');
            string aasPath = splitPath[0];
            string subPath = "";
            int i = 1;
            string pre = "";
            while (i < splitPath.Length)
            {
                subPath += pre + splitPath[i];
                pre = ".";
                i++;
            }

            if (status != null)
                status.value = "OK";
            if (opName == "get" || opName == "getdiff")
            {
                if (splitPath.Length < 2)
                    return;

                AdminShell.SubmodelElementCollection diffCollection = null;
                DateTime last = new DateTime();
                if (opName == "getdiff")
                {
                    if (lastDiff == null)
                        return;
                    if (elementCollection == null)
                        return;

                    if (lastDiff.value == "")
                    {
                        opName = "get";
                        lastDiff.value = "" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        requestPath = endPoint.value + "/aas/" + aasPath +
                            "/submodels/" + splitPath[1] + "/elements";
                        i = 2;
                        while (i < splitPath.Length)
                        {
                            requestPath += "/" + splitPath[i];
                            i++;
                        }
                        requestPath += "/complete";
                    }
                    else
                    {
                        last = DateTime.Parse(lastDiff.value);
                        requestPath = endPoint.value +
                            "/diffjson/aas/" + splitPath[0] +
                            "?path=" + subPath;
                        requestPath += "."; // to avoid wrong data by prefix only
                        requestPath += "&time=" + lastDiff.value;
                    }
                }

                try
                {
                    task = Task.Run(async () => { response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead); });
                    task.Wait();
                    if (!response.IsSuccessStatusCode)
                    {
                        if (status != null)
                        {
                            status.value = response.StatusCode.ToString() + " ; " +
                                response.Content.ReadAsStringAsync().Result;
                            Program.signalNewData(1);
                        }
                        return;
                    }
                }
                catch
                {
                    return;
                }

                string json = response.Content.ReadAsStringAsync().Result;
                AdminShell.SubmodelElementCollection receiveCollection = null;
                AdminShell.Submodel receiveSubmodel = null;
                try
                {
                    if (opName == "get")
                    {
                        if (elementCollection != null)
                        {
                            JObject parsed = JObject.Parse(json);
                            foreach (JProperty jp1 in (JToken)parsed)
                            {
                                if (jp1.Name == "elem")
                                {
                                    string text = jp1.Value.ToString();
                                    receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElementCollection>(
                                        text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                    elementCollection.value = receiveCollection.value;
                                    elementCollection.SetAllParentsAndTimestamps(elementCollection, timeStamp, elementCollection.TimeStampCreate);
                                    elementCollection.setTimeStamp(timeStamp);
                                }
                            }
                        }
                        if (elementSubmodel != null)
                        {
                            receiveSubmodel = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.Submodel>(
                                json, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                            receiveSubmodel.setTimeStamp(timeStamp);
                            receiveSubmodel.SetAllParents(timeStamp);

                            // need id for idempotent behaviour
                            if (receiveSubmodel.identification == null || receiveSubmodel.identification.id != elementSubmodel.identification.id)
                                return;

                            var aas = Program.env[envIndex].AasEnv.FindAASwithSubmodel(receiveSubmodel.identification);

                            // datastructure update
                            if (Program.env == null || Program.env[envIndex].AasEnv == null || Program.env[envIndex].AasEnv.Assets == null)
                                return;

                            // add Submodel
                            var existingSm = Program.env[envIndex].AasEnv.FindSubmodel(receiveSubmodel.identification);
                            if (existingSm != null)
                                Program.env[envIndex].AasEnv.Submodels.Remove(existingSm);
                            Program.env[envIndex].AasEnv.Submodels.Add(receiveSubmodel);
                        }
                    }
                    if (opName == "getdiff")
                    {
                        lastDiff.value = "" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        List<AasxRestServer.TestResource.diffEntry> diffList = new List<AasxRestServer.TestResource.diffEntry>();
                        diffList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AasxRestServer.TestResource.diffEntry>>(json);
                        foreach (var d in diffList)
                        {
                            if (d.type == "SMEC")
                            {
                                if (d.path.Length > subPath.Length && subPath == d.path.Substring(0, subPath.Length))
                                {
                                    splitPath = d.path.Split('.');
                                    if (splitPath.Length < 2)
                                        return;
                                    requestPath = endPoint.value + "/aas/" + aasPath +
                                        "/submodels/" + splitPath[0] + "/elements";
                                    i = 1;
                                    while (i < splitPath.Length)
                                    {
                                        requestPath += "/" + splitPath[i];
                                        i++;
                                    }
                                    requestPath += "/complete";
                                    try
                                    {
                                        task = Task.Run(async () => { response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead); });
                                        task.Wait();
                                        if (!response.IsSuccessStatusCode)
                                        {
                                            if (status != null)
                                            {
                                                status.value = response.StatusCode.ToString() + " ; " +
                                                    response.Content.ReadAsStringAsync().Result;
                                                Program.signalNewData(1);
                                            }
                                            return;
                                        }
                                    }
                                    catch
                                    {
                                        return;
                                    }

                                    json = response.Content.ReadAsStringAsync().Result;
                                    JObject parsed = JObject.Parse(json);
                                    foreach (JProperty jp1 in (JToken)parsed)
                                    {
                                        if (jp1.Name == "elem")
                                        {
                                            string text = jp1.Value.ToString();
                                            receiveCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<AdminShell.SubmodelElementCollection>(
                                                text, new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                                            break;
                                        }
                                    }
                                    switch (d.mode)
                                    {
                                        case "CREATE":
                                        case "UPDATE":
                                            bool found = false;
                                            foreach (var smew in elementCollection.value)
                                            {
                                                var sme = smew.submodelElement;
                                                if (sme.idShort == receiveCollection.idShort)
                                                {
                                                    if (sme is AdminShell.SubmodelElementCollection smc)
                                                    {
                                                        if (d.mode == "UPDATE")
                                                        {
                                                            smc.value = receiveCollection.value;
                                                            smc.SetAllParentsAndTimestamps(elementCollection, timeStamp, elementCollection.TimeStampCreate);
                                                            smc.setTimeStamp(timeStamp);
                                                        }
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (!found && d.mode == "CREATE")
                                            {
                                                elementCollection.value.Add(receiveCollection);
                                                receiveCollection.SetAllParentsAndTimestamps(elementCollection, timeStamp, timeStamp);
                                                receiveCollection.setTimeStamp(timeStamp);
                                                Program.signalNewData(2);
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    return;
                }
                Program.signalNewData(1);
            }
            if (opName == "put" || opName == "putdiff")
            {
                AdminShell.SubmodelElementCollection diffCollection = null;
                DateTime last = new DateTime();
                int count = 1;
                if (opName == "putdiff")
                {
                    if (lastDiff == null)
                        return;
                    if (elementCollection == null)
                        return;
                    diffCollection = elementCollection;
                    count = elementCollection.value.Count;
                    if (lastDiff.value == "")
                    {
                        opName = "put";
                        lastDiff.value = "" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    }
                    else
                    {
                        last = DateTime.Parse(lastDiff.value);
                    }
                }

                for (i = 0; i < count; i++)
                {
                    bool error = false;
                    string statusValue = "";
                    string json = "";
                    try
                    {
                        if (opName == "putdiff")
                        {
                            var sme = diffCollection.value[i].submodelElement;
                            if (!(sme is AdminShell.SubmodelElementCollection))
                                return;
                            elementCollection = sme as AdminShell.SubmodelElementCollection;
                            diffPath = "/" + diffCollection.idShort;
                            if (elementCollection.TimeStamp <= last)
                                elementCollection = null;
                        }
                        if (elementCollection != null)
                            json = JsonConvert.SerializeObject(elementCollection, Formatting.Indented);
                        if (elementSubmodel != null)
                            json = JsonConvert.SerializeObject(elementSubmodel, Formatting.Indented);
                    }
                    catch
                    {
                        statusValue = "error PUTDIFF index: " + i + " old " + count +
                            " new " + diffCollection.value.Count;
                        Console.WriteLine(statusValue);
                        error = true;
                    }
                    if (json != "")
                    {
                        try
                        {
                            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                            task = Task.Run(async () =>
                            {
                                response = await client.PutAsync(
                                    requestPath + diffPath, content);
                            });
                            task.Wait();
                        }
                        catch
                        {
                            error = true;
                        }
                        if (error || !response.IsSuccessStatusCode)
                        {
                            statusValue = response.StatusCode.ToString() + " ; " +
                                response.Content.ReadAsStringAsync().Result;
                            error = true;
                        }
                    }
                    if (error)
                    {
                        if (status != null)
                        {
                            status.value = statusValue;
                            Program.signalNewData(1);
                        }
                        return;
                    }
                }

                if (opName == "putdiff")
                {
                    lastDiff.value = "" + DateTime.UtcNow;
                }
            }
        }
        static void operation_limitCount(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            // inputVariable reference collection: collection
            // inputVariable limit: property
            // inputVariable prefix: property

            if (op.inputVariable.Count != 3)
            {
                return;
            }
            AdminShell.SubmodelElementCollection collection = null;
            AdminShell.Property limit = null;
            AdminShell.Property prefix = null;
            AdminShell.SubmodelElementCollection smec = null;
            AdminShell.Property p = null;

            foreach (var input in op.inputVariable)
            {
                smec = null;
                p = null;
                var inputRef = input.value.submodelElement;
                if (inputRef is AdminShell.Property)
                {
                    p = (inputRef as AdminShell.Property);
                }
                if (inputRef is AdminShell.ReferenceElement)
                {
                    var refElement = Program.env[envIndex].AasEnv.FindReferableByReference((inputRef as AdminShell.ReferenceElement).value);
                    if (refElement is AdminShell.SubmodelElementCollection)
                        smec = refElement as AdminShell.SubmodelElementCollection;
                }
                switch (inputRef.idShort.ToLower())
                {
                    case "collection":
                        if (smec != null)
                            collection = smec;
                        break;
                    case "limit":
                        if (p != null)
                            limit = p;
                        break;
                    case "prefix":
                        if (p != null)
                            prefix = p;
                        break;
                }
            }
            if (collection == null || limit == null || prefix == null)
                return;

            try
            {
                int count = Convert.ToInt32(limit.value);
                string pre = prefix.value;
                int preCount = 0;
                int i = 0;
                while (i < collection.value.Count)
                {
                    if (pre == collection.value[i].submodelElement.idShort.Substring(0, pre.Length))
                        preCount++;
                    i++;
                }
                i = 0;
                while (preCount > count && i < collection.value.Count)
                {
                    if (pre == collection.value[i].submodelElement.idShort.Substring(0, pre.Length))
                    {
                        AdminShell.Referable r = collection.value[i].submodelElement;
                        var sm = r.getParentSubmodel();
                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                            r, "Remove", sm, (ulong)timeStamp.Ticks);
                        collection.value.RemoveAt(i);
                        preCount--;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch
            {
            }
            Program.signalNewData(1);
        }

        static void operation_calculate_cfp(AdminShell.Operation op, int envIndex, DateTime timeStamp)
        {
            double cfpCradleToGateSum = 0.0;
            double cfpProductionSum = 0.0;
            double cfpDistributionSum = 0.0;
            AdminShell.Property cfpCradleToGate = null;
            AdminShell.Property cfpProduction = null;
            AdminShell.Property cfpDistribution = null;
            List<string> bomAssetId = new List<string>();

            // get cfp property and list of asset ids
            var env = AasxServer.Program.env[envIndex];
            if (env != null)
            {
                var aas = env.AasEnv.AdministrationShells[0];
                if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                {
                    foreach (var smr in aas.submodelRefs)
                    {
                        var sm = env.AasEnv.FindSubmodel(smr);
                        if (sm != null && sm.idShort != null)
                        {
                            if (sm.idShort == "ProductCarbonFootprint")
                            {
                                foreach (var v in sm.submodelElements)
                                {
                                    if (v.submodelElement is AdminShell.Property p)
                                    {
                                        switch (p.idShort)
                                        {
                                            case "CfpCradleToGate":
                                                cfpCradleToGate = p;
                                                break;
                                            case "CfpProduction":
                                                cfpProduction = p;
                                                break;
                                            case "CfpDistribution":
                                                cfpDistribution = p;
                                                break;
                                        }
                                    }
                                }
                            }
                            if (sm.idShort == "BillOfMaterial")
                            {
                                foreach (var v in sm.submodelElements)
                                {
                                    if (v.submodelElement is AdminShell.Entity e)
                                    {
                                        string s = "";
                                        s = e?.assetRef?.Keys?[0].value;
                                        if (s != "")
                                            bomAssetId.Add(s);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                env = AasxServer.Program.env[i];
                if (i != envIndex && env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    var assetId = aas.assetRef.Keys[0].value;
                    if (!bomAssetId.Contains(assetId))
                        continue;
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null)
                            {
                                if (sm.idShort == "ProductCarbonFootprint")
                                {
                                    foreach (var v in sm.submodelElements)
                                    {
                                        if (v.submodelElement is AdminShell.Property p)
                                        {
                                            double value = 0.0;
                                            try
                                            {
                                                value = Convert.ToDouble(p.value);
                                            }
                                            catch { }
                                            switch (p.idShort)
                                            {
                                                case "CfpCradleToGate":
                                                    cfpCradleToGateSum += value;
                                                    break;
                                                case "CfpProduction":
                                                    cfpProductionSum += value;
                                                    break;
                                                case "CfpDistribution":
                                                    cfpDistributionSum += value;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (cfpCradleToGate != null)
            {
                string value = cfpCradleToGateSum.ToString("N8");
                if (value != cfpCradleToGate.value)
                {
                    cfpCradleToGate.value = value;
                    cfpCradleToGate.setTimeStamp(timeStamp);
                }
            }
            if (cfpProduction != null)
            {
                string value = cfpProductionSum.ToString("N8");
                if (value != cfpProduction.value)
                {
                    cfpProduction.value = value;
                    cfpProduction.setTimeStamp(timeStamp);
                }
            }
            if (cfpDistribution != null)
            {
                string value = cfpDistributionSum.ToString("N8");
                if (value != cfpDistribution.value)
                {
                    cfpDistribution.value = value;
                    cfpDistribution.setTimeStamp(timeStamp);
                }
            }
            Program.signalNewData(1);
        }

        static Thread tasksThread;
        public static void tasksSamplingLoop()
        {
            while (true)
            {
                tasksCyclic();
                Thread.Sleep(100);
            }
        }

        public static void tasksCyclic()
        {
            if (Program.isLoading)
                return;

            DateTime timeStamp = DateTime.UtcNow;

            foreach (var t in taskList)
            {
                if (t.taskType?.value.ToLower() == "cyclic")
                {
                    if (t.nextExecution > timeStamp)
                        continue;
                    if (t.cycleCount != null)
                    {
                        if (t.cycleCount.value == "")
                            t.cycleCount.value = "0";
                        t.cycleCount.value = (Convert.ToInt32(t.cycleCount.value) + 1).ToString();
                        t.cycleCount.setTimeStamp(timeStamp);
                    }
                    t.nextExecution = timeStamp.AddMilliseconds(Convert.ToInt32(t.cycleTime.value));
                    if (t.nextCycle != null)
                    {
                        t.nextCycle.value = t.nextExecution.ToString();
                        t.nextCycle.setTimeStamp(timeStamp);
                    }
                    Program.signalNewData(0);

                    runOperations(t.def, t.envIndex, timeStamp);
                }
            }
        }
    }
}
