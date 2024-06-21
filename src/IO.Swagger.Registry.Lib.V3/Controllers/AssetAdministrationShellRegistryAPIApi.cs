/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification
 *
 * The Full Profile of the Asset Administration Shell Registry Service Specification as part of the [Specification of the Asset Administration Shell: Part 2](http://industrialdigitaltwin.org/en/content-hub).   Publisher: Industrial Digital Twin Association (IDTA) 2023
 *
 * OpenAPI spec version: V3.0.1_SSP-001
 * Contact: info@idtwin.org
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using AasxServer;
using AasxServerDB;
using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Attributes;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ ApiController ]
    public class AssetAdministrationShellRegistryAPIApiController : ControllerBase
    {
        private readonly IAppLogger<AssetAdministrationShellRegistryAPIApiController> _logger;
        private readonly IBase64UrlDecoderService _decoderService;
        private readonly IAasRegistryService _aasRegistryService;
        private readonly IRegistryInitializerService _registryInitializerService;
        private readonly IAasDescriptorPaginationService _paginationService;

        public AssetAdministrationShellRegistryAPIApiController(IAppLogger<AssetAdministrationShellRegistryAPIApiController> logger, IBase64UrlDecoderService decoderService,
            IAasRegistryService aasRegistryService, IRegistryInitializerService registryInitializerService, IAasDescriptorPaginationService paginationService)
        {
            _logger = logger;
            _decoderService = decoderService;
            _aasRegistryService = aasRegistryService;
            _registryInitializerService = registryInitializerService;
            _paginationService = paginationService;
        }

        /// <summary>
        /// Deletes an Asset Administration Shell Descriptor, i.e. de-registers an AAS
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell Descriptor deleted successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpDelete ]
        [ Route("/shell-descriptors/{aasIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("DeleteAssetAdministrationShellDescriptorById") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult DeleteAssetAdministrationShellDescriptorById([ FromRoute ] [ Required ] byte[] aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a Submodel Descriptor, i.e. de-registers a submodel
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Submodel Descriptor deleted successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpDelete ]
        [ Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("DeleteSubmodelDescriptorByIdThroughSuperpath") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult DeleteSubmodelDescriptorByIdThroughSuperpath([ FromRoute ] [ Required ] string aasIdentifier,
            [ FromRoute ] [ Required ] string submodelIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all Asset Administration Shell Descriptors
        /// </summary>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="assetKind">The Asset&#x27;s kind (Instance or Type)</param>
        /// <param name="assetType">The Asset&#x27;s type (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell Descriptors</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpGet ]
        [ Route("/shell-descriptors") ]
        [ ValidateModelState ]
        [ SwaggerOperation("GetAllAssetAdministrationShellDescriptors") ]
        [ SwaggerResponse(statusCode: 200, type: typeof(List<AssetAdministrationShellDescriptor>), description: "Requested Asset Administration Shell Descriptors") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult GetAllAssetAdministrationShellDescriptors([ FromQuery ] int? limit, [ FromQuery ] string? cursor, [ FromQuery ] string? assetKind,
            [ FromQuery ] string? assetType)
        {
            // TODO (jtikekar, 2023-09-04): AssetType resembles GlobalAssetId from old Implementation
            List<string?>? assetList = new List<string?>();
            if (!string.IsNullOrEmpty(assetType))
            {
                var decodedAssetType = _decoderService.Decode("assetType", assetType);
                assetList = new List<string?>()
                {
                    decodedAssetType
                };
            }

            List<AssetAdministrationShellDescriptor> aasDescriptors;
            if (!Program.withDb)
            {
                // from AAS memory
                aasDescriptors = _aasRegistryService.GetAllAssetAdministrationShellDescriptors(assetKind, assetList);
            }
            else
            {
                //From DB
                aasDescriptors = new List<AssetAdministrationShellDescriptor>();
                using (AasContext db = new AasContext())
                {
                    foreach (var aasDB in db.AASSets)
                    {
                        if (assetList.Count == 0 || assetList.Contains(aasDB.GlobalAssetId))
                        {
                            var aasDesc = _aasRegistryService.CreateAasDescriptorFromDB(aasDB);
                            aasDescriptors.Add(aasDesc);
                        }
                    }
                }
            }

            var output = _paginationService.GetPaginatedList(aasDescriptors, new Models.PaginationParameters(cursor, limit));
            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns all Submodel Descriptors
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Submodel Descriptors</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpGet ]
        [ Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors") ]
        [ ValidateModelState ]
        [ SwaggerOperation("GetAllSubmodelDescriptorsThroughSuperpath") ]
        [ SwaggerResponse(statusCode: 200, type: typeof(List<SubmodelDescriptor>), description: "Requested Submodel Descriptors") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult GetAllSubmodelDescriptorsThroughSuperpath([ FromRoute ] [ Required ] string aasIdentifier, [ FromQuery ] int? limit,
            [ FromQuery ] string cursor)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(GetSubmodelDescriptorsResult));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = "\"\"";

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a specific Asset Administration Shell Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell Descriptor</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpGet ]
        [ Route("/shell-descriptors/{aasIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("GetAssetAdministrationShellDescriptorById") ]
        [ SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShellDescriptor), description: "Requested Asset Administration Shell Descriptor") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult GetAssetAdministrationShellDescriptorById([ FromRoute ] [ Required ] string? aasIdentifier)
        {
            var decodedAasIdentifier = _decoderService.Decode("aasIdentifier", aasIdentifier);
            _logger.LogInformation($"Received request to get the AAS Descriptor by Id");
            if (!Program.withDb)
            {
                var aasList = _aasRegistryService.GetAllAssetAdministrationShellDescriptors(aasIdentifier: decodedAasIdentifier);
                if (aasList.Any())
                {
                    return new ObjectResult(aasList.First());
                }
            }
            else
            {
                // from database
                using (AasContext db = new AasContext())
                {
                    foreach (var aasDB in db.AASSets)
                    {
                        if (decodedAasIdentifier.Equals(aasDB.Identifier))
                        {
                            var aasDesc = _aasRegistryService.CreateAasDescriptorFromDB(aasDB);
                            return new ObjectResult(aasDesc);
                        }
                    }
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Returns a specific Submodel Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Submodel Descriptor</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpGet ]
        [ Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("GetSubmodelDescriptorByIdThroughSuperpath") ]
        [ SwaggerResponse(statusCode: 200, type: typeof(SubmodelDescriptor), description: "Requested Submodel Descriptor") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult GetSubmodelDescriptorByIdThroughSuperpath([ FromRoute ] [ Required ] string aasIdentifier,
            [ FromRoute ] [ Required ] string submodelIdentifier)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(SubmodelDescriptor));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new Asset Administration Shell Descriptor, i.e. registers an AAS
        /// </summary>
        /// <param name="body">Asset Administration Shell Descriptor object</param>
        /// <response code="201">Asset Administration Shell Descriptor created successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        //TODO (jtikekar, 2023-09-04): Routes are different than old impl
        [ HttpPost ]
        [ Route("/shell-descriptors") ]
        [ ValidateModelState ]
        [ SwaggerOperation("PostAssetAdministrationShellDescriptor") ]
        [ SwaggerResponse(statusCode: 201, type: typeof(AssetAdministrationShellDescriptor), description: "Asset Administration Shell Descriptor created successfully") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 409, type: typeof(Result),
            description:
            "Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult PostAssetAdministrationShellDescriptor([ FromBody ] AssetAdministrationShellDescriptor body)
        {
            var timestamp = DateTime.UtcNow;

            _logger.LogInformation($"Received request to create a new AAS Descriptor");

            // TODO (jtikekar, 2023-09-04): Just for testing purpose. Remove during refactoring
            bool test = false;
            if (test)
            {
                string jsonAd = DescriptorSerializer.ToJsonObject(body).ToJsonString();
                _logger.LogDebug(jsonAd);
            }

            lock (Program.changeAasxFile)
            {
                _registryInitializerService.CreateAssetAdministrationShellDescriptor(body, timestamp);
            }

            return CreatedAtAction("PostAssetAdministrationShellDescriptor", body);
        }

        /// <summary>
        /// Creates a new Submodel Descriptor, i.e. registers a submodel
        /// </summary>
        /// <param name="body">Submodel Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="201">Submodel Descriptor created successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpPost ]
        [ Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors") ]
        [ ValidateModelState ]
        [ SwaggerOperation("PostSubmodelDescriptorThroughSuperpath") ]
        [ SwaggerResponse(statusCode: 201, type: typeof(SubmodelDescriptor), description: "Submodel Descriptor created successfully") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 409, type: typeof(Result),
            description:
            "Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult PostSubmodelDescriptorThroughSuperpath([ FromBody ] SubmodelDescriptor body, [ FromRoute ] [ Required ] string aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(SubmodelDescriptor));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 409 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(409, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates an existing Asset Administration Shell Descriptor
        /// </summary>
        /// <param name="body">Asset Administration Shell Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell Descriptor updated successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpPut ]
        [ Route("/shell-descriptors/{aasIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("PutAssetAdministrationShellDescriptorById") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult PutAssetAdministrationShellDescriptorById([ FromBody ] AssetAdministrationShellDescriptor body,
            [ FromRoute ] [ Required ] byte[] aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates an existing Submodel Descriptor
        /// </summary>
        /// <param name="body">Submodel Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Submodel Descriptor updated successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [ HttpPut ]
        [ Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}") ]
        [ ValidateModelState ]
        [ SwaggerOperation("PutSubmodelDescriptorByIdThroughSuperpath") ]
        [ SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.") ]
        [ SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden") ]
        [ SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found") ]
        [ SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error") ]
        [ SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes") ]
        public virtual IActionResult PutSubmodelDescriptorByIdThroughSuperpath([ FromBody ] SubmodelDescriptor body, [ FromRoute ] [ Required ] string aasIdentifier,
            [ FromRoute ] [ Required ] string submodelIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        [ HttpPost ]
        [ Route("/overwrite-shell-descriptors") ]
        [ ValidateModelState ]
        [ SwaggerOperation("PostMultipleAssetAdministrationShellDescriptors") ]
        [ SwaggerResponse(statusCode: 201, type: typeof(List<AssetAdministrationShellDescriptor>), description: "Asset Administration Shell Descriptors created successfully") ]
        public virtual IActionResult PostMultipleAssetAdministrationShellDescriptor([ FromBody ] List<AssetAdministrationShellDescriptor> body)
        {
            var timestamp = DateTime.UtcNow;

            _logger.LogDebug($"Received request to create multiple shell descriptors");

            _registryInitializerService.CreateMultipleAssetAdministrationShellDescriptor(body, timestamp);

            return new ObjectResult("ok");
        }

        /// <summary>
        /// Returns a list of Asset Administration Shell ids based on Asset identifier key-value-pairs
        /// </summary>
        /// <param name="assetIds">The key-value-pair of an Asset identifier (BASE64-URL-encoded JSON-serialized key-value-pairs)</param>
        /// <param name="assetId">An Asset identifier (BASE64-URL-encoded identifier)</param>
        /// <response code="200">Requested Asset Administration Shell ids</response>
        [ HttpGet ]
        [ Route("/lookup/shells") ]
        [ ValidateModelState ]
        [ SwaggerOperation("GetAllAssetAdministrationShellIdsByAssetLink") ]
        [ SwaggerResponse(statusCode: 200, type: typeof(List<string>), description: "Requested Asset Administration Shell ids") ]
        public virtual IActionResult GetAllAssetAdministrationShellIdsByAssetLink(
            [ FromQuery ] List<SpecificAssetId> assetIds,
            [ FromQuery ] string? assetId)
        {
            try
            {
                //collect aasetIds from list
                var assetList = new List<string?>();
                foreach (var kv in assetIds)
                {
                    if (kv.Value != "")
                    {
                        var decodedAssetId = _decoderService.Decode("assetId", kv.Value);
                        assetList.Add(decodedAssetId);
                    }
                }

                // single assetId
                if (assetId != null && assetId != "")
                {
                    var decodedAssetId = _decoderService.Decode("assetId", assetId);
                    assetList.Add(decodedAssetId);
                }

                var aasList = new List<string?>();

                if (!Program.withDb)
                {
                    var aasDecsriptorList = _aasRegistryService.GetAllAssetAdministrationShellDescriptors(assetList: assetList);

                    foreach (var ad in aasDecsriptorList)
                    {
                        if (ad != null)
                        {
                            aasList.Add(ad.Id);
                        }
                    }
                }
                else
                {
                    // from database
                    using (AasContext db = new AasContext())
                    {
                        foreach (var aasDB in db.AASSets)
                        {
                            if (assetList.Contains(aasDB.GlobalAssetId))
                                aasList.Add(aasDB.Identifier);
                        }
                    }
                }

                return new ObjectResult(aasList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}