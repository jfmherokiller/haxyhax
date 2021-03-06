using System.Collections.Generic;
using System.IO;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.CommonJS
{
    public class Module : IModule
    {
        /// <summary>
        /// This module's children
        /// </summary>
        public List<IModule> Children { get; }

        protected Module parentModule;

        protected CJSJintEngine engine;

        /// <summary>
        /// Determines if this module is the main module.
        /// </summary>
        public bool isMainModule => this.parentModule == null;

        public string Id { get; set; }

        /// <summary>
        /// Contains the module's public API.
        /// </summary>
        public JsValue Exports { get; set; }

        public readonly string filePath;

        /// <summary>
        /// Creates a new Module instaznce with the specified module id. The module is resolved to a file on disk
        /// according to the CommonJS specification.
        /// </summary>
        internal Module(CJSJintEngine e, string moduleId, Module parent = null)
        {
            if (e == null)
            {
                throw new System.ArgumentNullException(nameof(e));
            }

            this.engine = e;
            Children = new List<IModule>();

            if (string.IsNullOrEmpty(moduleId))
            {
                throw new System.ArgumentException("A moduleId is required.", nameof(moduleId));
            }

            Id = moduleId;
            this.filePath = e.Resolver.ResolvePath(Id, this);
            this.parentModule = parent;

            if (parent != null)
            {
                parent.Children.Add(this);
            }

            this.Exports = engine.Object.Construct(new JsValue[] { });

            string extension = Path.GetExtension(this.filePath);
            var loader = this.engine.FileExtensionParsers[extension] ?? this.engine.FileExtensionParsers["default"];

            e.ModuleCache[Id] = this;

            loader(this.filePath, this);
        }

        protected JsValue Require(string moduleId)
        {
            return engine.Load(moduleId,isMainModule, this);
        }

        public JsValue Compile(string sourceCode, string filePath)
        {
            var moduleObject = JsValue.FromObject(this.engine, this);

            // moduleObject.AsObject().DefineOwnProperty("exports", new Runtime.Descriptors.PropertyDescriptor() {
            //     Get = new ClrFunctionInstance(engine.engine, (thisObj, args) => Exports),
            //     Set = new ClrFunctionInstance(engine.engine, (thisObj, args) => Exports = args.At(0)),
            //     Enumerable = true,
            //     Configurable = true,
            // }, throwOnError: true);

            engine.Execute($@"
                ;(function (module, exports, __dirname, require) {{
                    {sourceCode}
                }})
            ").GetCompletionValue().As<FunctionInstance>().Call(
                JsValue.FromObject(engine, this),
                new JsValue[]
                {
                    moduleObject,
                    this.Exports,
                    Path.GetDirectoryName(filePath),
                    new ClrFunctionInstance(engine, (thisObj, arguments) => Require(arguments.At(0).AsString()))
                    //  new DelegateWrapper(engine.engine, new Func<string, JsValue>(this.Require)),
                }
            );

            return Exports;
        }
    }
}