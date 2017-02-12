using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using System;
using System.Net;
using System.ServiceModel.Description;
using System.Configuration;

namespace CRMConnectionHelper
{
    public class XrmConnectionProvider
    {
        private static IOrganizationService instance;
        private static object _lockObject = new object();

        private static IConfigurationService _configurationService;

        private XrmConnectionProvider() { }


        public static IOrganizationService CRMService
        {
            get
            {
                try
                {
                    lock (_lockObject)
                    {
                        if (instance == null)
                        {
                            var container = IoC.Initialize();
                            _configurationService = container.GetInstance<IConfigurationService>();


                            instance = Connect();
                        }
                        return instance;
                    }
                }
                catch (Exception ex)
                {

                    throw new Exception("Unable to connect to CRM", ex);
                }

            }
        }
        private static IOrganizationService Connect()
        {
            var config = _configurationService.Get<XrmClientConfiguration>();


            Uri dInfo = new Uri(config.XrmUri);
            ClientCredentials clientcred = new ClientCredentials();
            clientcred.UserName.UserName = config.XrmClientCredUserName;
            clientcred.UserName.Password = config.XrmClientCredPassword;


            #region on-premise/online

            DiscoveryServiceProxy dsp = new DiscoveryServiceProxy(dInfo, null, clientcred, null);
            dsp.Authenticate();
            RetrieveOrganizationsRequest rosreq = new Microsoft.Xrm.Sdk.Discovery.RetrieveOrganizationsRequest();
            RetrieveOrganizationsResponse r = (RetrieveOrganizationsResponse)dsp.Execute(rosreq);

            //get the OrganizationService
            ManagedTokenOrganizationServiceProxy _serviceproxy = new ManagedTokenOrganizationServiceProxy(new Uri(config.XrmOrgServiceProxy), clientcred);
            //In order to use the generated types when dealing with the organization service, you have to add the ProxyTypesBehavior to the endpoint Behaviors collection. This instructs the OrganizationServiceProxy to look in the assembly for classes with certain attributes to use. The generated classes are already attributed with these attributes. Simply, this makes all interactions with the organization service to be done using the generated typed classes for each entity instead of the generic Entity class we used earlier.
            _serviceproxy.ServiceConfiguration.CurrentServiceEndpoint.EndpointBehaviors.Add(new ProxyTypesBehavior());
            //Do not forget to include _serviceproxy.EnableProxyTypes();. Without this line,you will be unable to use early binding.
            _serviceproxy.EnableProxyTypes();
            IOrganizationService service = (IOrganizationService)_serviceproxy;

            #endregion
            return service;
        }
    }
}