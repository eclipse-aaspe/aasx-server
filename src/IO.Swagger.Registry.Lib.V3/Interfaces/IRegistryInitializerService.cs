﻿using AasxServer;
using IO.Swagger.Registry.Lib.V3.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Registry.Lib.V3.Interfaces
{
    public interface IRegistryInitializerService
    {
        void       CreateAssetAdministrationShellDescriptor(AssetAdministrationShellDescriptor newAasDesc, DateTime timestamp, bool initial = false);
        void       CreateMultipleAssetAdministrationShellDescriptor(List<AssetAdministrationShellDescriptor> body, DateTime timestamp);
        ISubmodel? GetAasRegistry();

        List<AssetAdministrationShellDescriptor> GetAasDescriptorsForSubmodelView();
        List<string> GetRegistryList();
        void InitRegistry(List<AasxCredentialsEntry> cList, DateTime timestamp, bool initAgain = false);
    }
}