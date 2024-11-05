﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GeoTagNinja.Resources.Languages {
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
    public class ToolTip {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ToolTip() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GeoTagNinja.Resources.Languages.ToolTip", typeof(ToolTip).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Press CTRL+V to paste a valid pair of decimals to the lat/long placeholders. 
        ///The clipboard should contain only a pair of decimals, preferably in &quot;invariant&quot; (English) setup, which is integer-dot-decimal-comma-integer-dot-decimal, such as 12.345, 56.897
        ///That said the code _should_ be able to comprehend other layouts. Note if the clipboard has anything apart from the above the operation will fail (nothing will happen.).
        /// </summary>
        public static string ttp_GPSDataPaste {
            get {
                return ResourceManager.GetString("ttp_GPSDataPaste", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Load metadata for the selected file(s) from your Favourites.
        /// </summary>
        public static string ttp_LoadFavourite {
            get {
                return ResourceManager.GetString("ttp_LoadFavourite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save the Current Coordinate to the Selected File(s).
        /// </summary>
        public static string ttp_loctToFile {
            get {
                return ResourceManager.GetString("ttp_loctToFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save the Current Coordinate to the Selected Files&apos; Destination.
        /// </summary>
        public static string ttp_loctToFileDestination {
            get {
                return ResourceManager.GetString("ttp_loctToFileDestination", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Manage Favourites.
        /// </summary>
        public static string ttp_ManageFavourites {
            get {
                return ResourceManager.GetString("ttp_ManageFavourites", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Put Coordinates of Selected Files onto the Map.
        /// </summary>
        public static string ttp_NavigateMapGo {
            get {
                return ResourceManager.GetString("ttp_NavigateMapGo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time Zones are left as blank on open regardless of what the stored value is. There is no Exif Tag for TZ but only &apos;Offset&apos;, which is something like &apos;+01:00&apos;. As there is no indication for neither TZ nor DST per se I can&apos;t ascertain that  &apos;+01:00&apos; was in fact say BST rather than CET, one being DST the other not. Either adjust manually or pull from web - the combination of coordinates + createDate would decisively inform the program of the real TZ value. The value in the read-only textbox will be saved in the [rest of string was truncated]&quot;;.
        /// </summary>
        public static string ttp_OffsetTime {
            get {
                return ResourceManager.GetString("ttp_OffsetTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save (or overwrite) the currently selected file&apos;s metadata to your Favourites.
        /// </summary>
        public static string ttp_SaveFavourite {
            get {
                return ResourceManager.GetString("ttp_SaveFavourite", resourceCulture);
            }
        }
    }
}
