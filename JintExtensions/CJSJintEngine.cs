using System;
using System.Collections.Generic;
using System.IO;
using Jint.CommonJS;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint
{
    public class CJSJintEngine : Engine
    {
        private void ConstructorCommon(IModuleResolver resolver)
        {
            this.Resolver = resolver;

            FileExtensionParsers.Add("default", this.LoadJS);
            FileExtensionParsers.Add(".js", this.LoadJS);
            FileExtensionParsers.Add(".json", this.LoadJson);

            if (resolver == null)
            {
                Resolver = new CommonJSPathResolver(this.FileExtensionParsers.Keys);
            }
            
            this.SetValue("require",new ClrFunctionInstance(this, (thisObj, arguments) => Require(arguments.At(0).AsString())));

        }

        public CJSJintEngine(Action<Options> options) : base(options)
        {
            ConstructorCommon(null);
        }

        public CJSJintEngine()
        {
            ConstructorCommon(null);
        }

        public CJSJintEngine(IModuleResolver resolver)
        {
            ConstructorCommon(resolver);
        }

        public CJSJintEngine(IModuleResolver resolver, Action<Options> options) : base(options)
        {
            ConstructorCommon(resolver);
        }

        public delegate JsValue FileExtensionParser(string path, IModule module);

        public Dictionary<string, IModule> ModuleCache = new Dictionary<string, IModule>();

        public Dictionary<string, FileExtensionParser> FileExtensionParsers =
            new Dictionary<string, FileExtensionParser>();

        public IModuleResolver Resolver { get; set; }

        private JsValue LoadJS(string path, IModule module)
        {
            var sourceCode = File.ReadAllText(path);
            if (module is Module)
            {
                module.Exports = (module as Module).Compile(sourceCode, path);
            }
            else
            {
                module.Exports = Execute(sourceCode).GetCompletionValue();
            }

            return module.Exports;
        }

        private JsValue LoadJson(string path, IModule module)
        {
            var sourceCode = File.ReadAllText(path);
            module.Exports = Json.Parse(JsValue.Undefined, new[] {JsValue.FromObject(this, sourceCode)}).AsObject();
            return module.Exports;
        }

        protected CJSJintEngine RegisterInternalModule(InternalModule mod)
        {
            ModuleCache.Add(mod.Id, mod);
            return this;
        }

        /// <summary>
        /// Registers an internal module to the provided delegate handler.
        /// </summary>
        public CJSJintEngine RegisterInternalModule(string id, Delegate d)
        {
            this.RegisterInternalModule(id, new DelegateWrapper(this, d));
            return this;
        }

        /// <summary>
        /// Registers an internal module under the specified id to the provided .NET CLR type.
        /// </summary>
        public CJSJintEngine RegisterInternalModule(string id, Type clrType)
        {
            this.RegisterInternalModule(id, TypeReference.CreateTypeReference(this, clrType));
            return this;
        }

        /// <summary>
        /// Registers an internal module under the specified id to any JsValue instance.
        /// </summary>
        public CJSJintEngine RegisterInternalModule(string id, JsValue value)
        {
            this.RegisterInternalModule(new InternalModule(id, value));
            return this;
        }

        public JsValue RunMain(string mainModuleName)
        {
            if (string.IsNullOrWhiteSpace(mainModuleName))
            {
                throw new System.ArgumentException("A Main module path is required.", nameof(mainModuleName));
            }

            return this.Load(mainModuleName);
        }

        public JsValue Load(string moduleName, Module parent = null)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new System.ArgumentException("moduleName is required.", nameof(moduleName));
            }

            IModule module = null;
            if (ModuleCache.ContainsKey(moduleName))
            {
                module = ModuleCache[moduleName];
                parent.Children.Add(module);
                return module.Exports;
            }

            return new Module(this, moduleName, parent).Exports;
        }
        protected JsValue Require(string moduleId)
        {
            return Load(moduleId);
        }
    }
}