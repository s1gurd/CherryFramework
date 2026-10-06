using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityCodeGen;
using UnityEngine;

namespace CherryFramework.DataModels.Editor
{
    [Generator]
    // ReSharper disable once UnusedType.Global
    public class ModelsGenerator : ICodeGenerator
    {
        public void Execute(GeneratorContext context)
        {
            Debug.Log("[Models Generator] Start...");
            context.OverrideFolderPath(CodeGenConstants.BasePath);
            
            if (Directory.Exists(Path.Combine(CodeGenConstants.BasePath, CodeGenConstants.ModelsPathAndNamespace)))
            {
                Directory.Delete(Path.Combine(CodeGenConstants.BasePath, CodeGenConstants.ModelsPathAndNamespace), true);
            }

            Directory.CreateDirectory(Path.Combine(CodeGenConstants.BasePath, CodeGenConstants.ModelsPathAndNamespace));
            
            var templates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace != null && t.Namespace.EndsWith(CodeGenConstants.TemplateNamespaceSuffix));
            
            foreach (var template in templates)
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

                var baseModelName = GetBaseName(template).Replace(CodeGenConstants.TemplateSuffix, String.Empty) + CodeGenConstants.ModelSuffix;
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

                var modelFilename = new StringBuilder();
                modelFilename.Append(Path.Combine(CodeGenConstants.ModelsPathAndNamespace, baseModelName));
                if (genericArguments.Length > 0)
                {
                    modelFilename.Append('.');
                    modelFilename.Append(string.Join(",", genericArguments.Select(x => x.Name)));
                }
                modelFilename.Append(CodeGenConstants.FilenameEnd);
                context.AddCode(modelFilename.ToString(), code);
            }
            
            
            

            Debug.Log("[Models Generator] Finished!");
        }

        private static string GetBaseName(Type type)
        {
            var index = type.Name.IndexOf('`');
            return index < 0 ? type.Name : type.Name.Remove(index);
        }

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
