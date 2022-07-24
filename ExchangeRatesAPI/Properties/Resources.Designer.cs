﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ExchangeRatesAPI.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ExchangeRatesAPI.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to yyyy-MM-dd.
        /// </summary>
        internal static string API_DATE_FORMAT {
            get {
                return ResourceManager.GetString("API_DATE_FORMAT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://sdw-wsrest.ecb.europa.eu/service/data/EXR/D.{0}.{1}.SP00.A?startPeriod={2}&amp;endPeriod={3}&amp;format=jsondata&amp;detail=dataonly.
        /// </summary>
        internal static string API_ENDPOINT {
            get {
                return ResourceManager.GetString("API_ENDPOINT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to +.
        /// </summary>
        internal static string API_STRING_CONCAT {
            get {
                return ResourceManager.GetString("API_STRING_CONCAT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Showing exception(s):.
        /// </summary>
        internal static string ERR_SHOWING_EXC {
            get {
                return ResourceManager.GetString("ERR_SHOWING_EXC", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Looks like the external API does not have exchange rate history for the requested day and past {0} day(s)..
        /// </summary>
        internal static string ERROR_API_NO_DATA {
            get {
                return ResourceManager.GetString("ERROR_API_NO_DATA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provided API key is wrong..
        /// </summary>
        internal static string ERROR_BAD_KEY {
            get {
                return ResourceManager.GetString("ERROR_BAD_KEY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Looks like there was an attempt to insert a rate that is already in the database. Did some just added it concurrently during this request?.
        /// </summary>
        internal static string ERROR_ITEM_ALREADY_IN_DB {
            get {
                return ResourceManager.GetString("ERROR_ITEM_ALREADY_IN_DB", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No API key has been provided..
        /// </summary>
        internal static string ERROR_NO_KEY {
            get {
                return ResourceManager.GetString("ERROR_NO_KEY", resourceCulture);
            }
        }
    }
}
