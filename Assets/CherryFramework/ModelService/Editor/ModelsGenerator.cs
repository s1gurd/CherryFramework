using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityCodeGen;
using UnityEditor;
using UnityEngine;

namespace CherryFramework.ModelService.Editor
{
    [Generator]
    // ReSharper disable once UnusedType.Global
    public class ModelsGenerator : ICodeGenerator
    {
        public void Execute(GeneratorContext context)
        {
            Debug.Log("[Models Generator] Start...");
            context.OverrideFolderPath(CodeGenConstants.ProjectRootPath);

            var templates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace != null && t.Namespace.EndsWith(CodeGenConstants.TemplateNamespaceSuffix));

            var outputs = new Dictionary<string, List<Type>>(StringComparer.Ordinal);

            foreach (var template in templates)
            {
                var folder = GetOutputFolder(template);

                if (folder == null)
                {
                    Debug.LogError($"[Models Generator] Could not find the source file of the template {template.FullName}. Its model goes to the fallback folder {CodeGenConstants.FallbackOutputFolder}.");
                    folder = CodeGenConstants.FallbackOutputFolder;
                }

                if (!outputs.TryGetValue(folder, out var list))
                {
                    list = new List<Type>();
                    outputs.Add(folder, list);
                }

                list.Add(template);
            }

            foreach (var output in outputs)
            {
                var folder = output.Key;
                var expected = new HashSet<string>(StringComparer.Ordinal);

                foreach (var template in output.Value)
                {
                    var fileName = GetModelFileName(template);
                    expected.Add(fileName);
                    context.AddCode($"{folder}/{fileName}", BuildModelCode(template));
                }

                RemoveStaleFiles(folder, expected);
            }

            Debug.Log("[Models Generator] Finished!");
        }

        /// <summary>
        /// The folder the model of the template is generated into: a
        /// "generated" folder next to the source file of the template.
        /// </summary>
        public static string GetOutputFolder(Type template)
        {
            var assetPath = FindTemplateAssetPath(template);

            if (assetPath == null)
                return null;

            var folder = Path.GetDirectoryName(assetPath);

            if (string.IsNullOrEmpty(folder))
                return null;

            return folder.Replace('\\', '/') + "/" + CodeGenConstants.GeneratedFolderName;
        }

        /// <summary>
        /// The namespace of the generated model: the namespace of the template
        /// with the "generated" suffix.
        /// </summary>
        public static string GetModelNamespace(Type template)
            => template.Namespace + CodeGenConstants.GeneratedNamespaceSuffix;

        /// <summary>
        /// The name of the generated file, with the generic arguments of the
        /// template in it.
        /// </summary>
        public static string GetModelFileName(Type template)
        {
            var builder = new StringBuilder();
            builder.Append(GetBaseModelName(template));

            if (template.IsGenericTypeDefinition)
            {
                builder.Append('.');
                builder.Append(string.Join(",", template.GetGenericArguments().Select(x => x.Name)));
            }

            builder.Append(CodeGenConstants.FilenameEnd);

            return builder.ToString();
        }

        /// <summary>
        /// Finds the source file of the template. The class of the script has
        /// to match the template, otherwise a class of the same name from
        /// another namespace is picked up instead.
        /// </summary>
        private static string FindTemplateAssetPath(Type template)
        {
            var baseName = GetBaseName(template);

            foreach (var guid in AssetDatabase.FindAssets($"{baseName} t:MonoScript"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(assetPath))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script == null)
                    continue;

                if (script.GetClass() == template)
                    return assetPath;

                if (DeclaresType(assetPath, template.Namespace, baseName))
                    return assetPath;
            }

            return null;
        }

        /// <summary>
        /// The fallback for a template whose class does not live in a file of
        /// the same name, where the class of the script cannot be resolved.
        /// </summary>
        private static bool DeclaresType(string assetPath, string templateNamespace, string baseName)
        {
            if (!File.Exists(assetPath))
                return false;

            var text = File.ReadAllText(assetPath);

            return text.Contains($"namespace {templateNamespace}") && text.Contains($"class {baseName}");
        }

        /// <summary>
        /// Removes the models of the templates that are gone. Only the files
        /// are removed, the folder and the meta files of the models that stay
        /// are kept, so their guids do not change on every run.
        /// </summary>
        private static void RemoveStaleFiles(string folder, HashSet<string> expected)
        {
            if (!Directory.Exists(folder))
                return;

            foreach (var file in Directory.GetFiles(folder, "*" + CodeGenConstants.FilenameEnd))
            {
                if (expected.Contains(Path.GetFileName(file)))
                    continue;

                File.Delete(file);

                var meta = file + ".meta";

                if (File.Exists(meta))
                    File.Delete(meta);
            }
        }

        private static string BuildModelCode(Type template)
        {
            var genericArguments = template.IsGenericTypeDefinition
                ? template.GetGenericArguments()
                : Array.Empty<Type>();
            var genericSuffix = genericArguments.Length > 0
                ? "<" + string.Join(", ", genericArguments.Select(x => x.Name)) + ">"
                : String.Empty;

            var code = CodeGenConstants.ModelTemplate;
            code = code.Replace(CodeGenConstants.TemplateNamePlaceholder, GetBaseName(template) + genericSuffix);
            code = code.Replace(CodeGenConstants.TemplateNamespacePlaceholder, template.Namespace);
            code = code.Replace(CodeGenConstants.NamespacePlaceholder, GetModelNamespace(template));

            var baseModelName = GetBaseModelName(template);
            var modelName = baseModelName + genericSuffix;
            code = code.Replace(CodeGenConstants.ModelNamePlaceholder, modelName);
            code = code.Replace(CodeGenConstants.ModelConstructorNamePlaceholder, baseModelName);
            code = code.Replace(CodeGenConstants.ConstraintsPlaceholder, BuildConstraints(template));

            var ctor = new StringBuilder();
            var props = new StringBuilder();
            var fields = template.GetFields();

            ctor.Append("\t\t\t_template = new();\n");
            foreach (var field in fields)
            {
                var attributes = field.CustomAttributes;
                var attributeString = new StringBuilder();
                foreach (var attribute in attributes)
                {
                    var attrName = attribute.ToString();

                    if (CodeGenConstants.CopyAttributes.Contains(attrName))
                    {
                        attributeString.Append(attrName);
                    }
                }
                var name = field.Name;
                var typeName = TypeUtils.GetFormattedName(field.FieldType);
                ctor.Append($"\t\t\tGetters.Add(nameof({name}), new Func<{typeName}>(() => {name}));\n");
                ctor.Append($"\t\t\tSetters.Add(nameof({name}), new Action<{typeName}>(o => {name} = o));\n");
                ctor.Append($"\t\t\t{name}Accessor = new Accessor<{typeName}>(this, nameof({name}));\n\n");

                var propHeader =
                    CodeGenConstants.PropertyTemplate.Replace(CodeGenConstants.MemberNamePlaceholder, name);
                propHeader = propHeader.Replace(CodeGenConstants.TypePlaceholder, typeName);
                propHeader = propHeader.Replace(CodeGenConstants.AttributesPlaceholder, attributeString.ToString());
                props.Append(propHeader);
            }

            code = code.Replace(CodeGenConstants.ConstructorPlaceholder, ctor.ToString());
            code = code.Replace(CodeGenConstants.PropsPlaceholder, props.ToString());

            return code;
        }

        private static string GetBaseName(Type type)
        {
            var index = type.Name.IndexOf('`');
            return index < 0 ? type.Name : type.Name.Remove(index);
        }

        private static string GetBaseModelName(Type template)
            => GetBaseName(template).Replace(CodeGenConstants.TemplateSuffix, String.Empty) + CodeGenConstants.ModelSuffix;

        private static string BuildConstraints(Type template)
        {
            if (!template.IsGenericTypeDefinition)
                return String.Empty;

            var parameterConstraints = new List<string>();
            foreach (var parameter in template.GetGenericArguments())
            {
                var constraints = new List<string>();
                var typeConstraints = parameter.GetGenericParameterConstraints();
                for (var i = 0; i < typeConstraints.Length; i++)
                {
                    constraints.Add(TypeUtils.GetFormattedName(typeConstraints[i]));
                }

                var attributes = parameter.GenericParameterAttributes;
                if ((attributes & System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    constraints.Add("class");
                else if ((attributes & System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                    constraints.Add("struct");
                if ((attributes & System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                    constraints.Add("new()");

                if (constraints.Count > 0)
                    parameterConstraints.Add($"where {parameter.Name} : {string.Join(", ", constraints)}");
            }

            return string.Join(" ", parameterConstraints);
        }
    }
}
