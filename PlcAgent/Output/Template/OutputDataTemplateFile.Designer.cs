﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace _PlcAgent.Output.Template {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    public sealed partial class OutputDataTemplateFile : global::System.Configuration.ApplicationSettingsBase {
        
        private static OutputDataTemplateFile defaultInstance = ((OutputDataTemplateFile)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new OutputDataTemplateFile())));
        
        public static OutputDataTemplateFile Default {
            get {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<ArrayOfString>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                        <string>Output\Template\Empty_Template.xml</string>
                                                                    </ArrayOfString>")]
        public string[] TemplateFiles
        {
            get { return ((string[])(this["TemplateFiles"])); }
            set { this["TemplateFiles"] = value; }
        }
    }
}
