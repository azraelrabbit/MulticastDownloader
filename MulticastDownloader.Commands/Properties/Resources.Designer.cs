﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MS.MulticastDownloader.Commands.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MS.MulticastDownloader.Commands.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to CTRL-C pressed....
        /// </summary>
        internal static string CtrlCPressed {
            get {
                return ResourceManager.GetString("CtrlCPressed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to download.
        /// </summary>
        internal static string DownloadActivity {
            get {
                return ResourceManager.GetString("DownloadActivity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to download status..
        /// </summary>
        internal static string DownloadStatus {
            get {
                return ResourceManager.GetString("DownloadStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to reception.
        /// </summary>
        internal static string ReceptionActivity {
            get {
                return ResourceManager.GetString("ReceptionActivity", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to reception status..
        /// </summary>
        internal static string ReceptionStatus {
            get {
                return ResourceManager.GetString("ReceptionStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source path not found..
        /// </summary>
        internal static string SourcePathNotFound {
            get {
                return ResourceManager.GetString("SourcePathNotFound", resourceCulture);
            }
        }
    }
}
