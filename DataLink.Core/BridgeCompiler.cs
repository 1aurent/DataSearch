﻿/* ........................................................................
 * copyright 2010 Laurent Dupuis
 * ........................................................................
 * < This program is free software: you can redistribute it and/or modify
 * < it under the terms of the GNU General Public License as published by
 * < the Free Software Foundation, either version 3 of the License, or
 * < (at your option) any later version.
 * < 
 * < This program is distributed in the hope that it will be useful,
 * < but WITHOUT ANY WARRANTY; without even the implied warranty of
 * < MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * < GNU General Public License for more details.
 * < 
 * < You should have received a copy of the GNU General Public License
 * < along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * ........................................................................
 *
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace DataLink.Core
{
    public class BridgeCompiler
    {
        static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();
        readonly ConstructorInfo _sObjectConstructor;
        readonly MethodInfo _siDbConnectionCreateCommand;
        readonly MethodInfo _siDbCommandSetCommandText;
        readonly MethodInfo _siDbCommandGetParameters;
        readonly MethodInfo _siDbCommandCreateParameter;
        readonly MethodInfo _siDataParameterSetParameterName;
        readonly MethodInfo _siDataParameterSetValue;
        readonly MethodInfo _siListAdd;

        class MethodCompileInfo
        {
            public MethodInfo Infos { get; set; }
            public string Sql { get; set; }
            public ParameterInfo[] Parms { get; set; }
            public Type RetEnumType { get; set; }

            public FieldBuilder CmdField { get; set; }
            public FieldBuilder PrsField { get; set; }
        }


        readonly List<MethodCompileInfo> _methods;
        readonly Type _t;
        private BridgeCompiler(Type t)
        {
            _sObjectConstructor = typeof(object).GetConstructor(new Type[0]);
            _siDbConnectionCreateCommand = typeof(IDbConnection).GetMethod("CreateCommand");
            _siDbCommandSetCommandText = typeof(IDbCommand).GetProperty("CommandText").GetSetMethod();
            _siDbCommandGetParameters = typeof(IDbCommand).GetProperty("Parameters").GetGetMethod();
            _siDbCommandCreateParameter = typeof(IDbCommand).GetMethod("CreateParameter");
            _siDataParameterSetParameterName = typeof(IDataParameter).GetProperty("ParameterName").GetSetMethod();
            _siDataParameterSetValue = typeof(IDataParameter).GetProperty("Value").GetSetMethod();
            _siListAdd = typeof(System.Collections.IList).GetMethod("Add");
            
            _t = t;

            _methods = new List<MethodCompileInfo>();

            foreach (var info in _t.GetMethods())
            {
                var sql = info.GetCustomAttributes(typeof(SqlStatementAttribute), true);
                if (sql.Length == 0) throw new ArgumentException("Method " + info.Name + " doesn't have a SQL statement");

                Type retEnum = null;
                if (info.ReturnType.Name == "IEnumerator`1")
                {
                    retEnum = info.ReturnType.GetGenericArguments()[0];
                }

                _methods.Add(new MethodCompileInfo { Infos = info, Sql = ((SqlStatementAttribute)sql[0]).Sql,
                                                     Parms = info.GetParameters(),
                                                     RetEnumType = retEnum
                });
            }
        }


        private void EmitMemberVars(TypeBuilder cType)
        {
            foreach (var method in _methods)
            {
                var mName = method.Infos.Name;
                method.CmdField = cType.DefineField("_cmd_" + mName, typeof(IDbCommand), FieldAttributes.Private);
                if (method.Parms.Length > 0)
                {
                    method.PrsField =
                        cType.DefineField("_params_" + mName, typeof(IDbDataParameter[]), FieldAttributes.Private);
                }
            }
        }

        private void EmitConstructor(TypeBuilder cType)
        {
            var cb = cType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                                             new[] { typeof(IDbConnection) });

            var il = cb.GetILGenerator();
            il.DeclareLocal(typeof(IDataParameterCollection));

            // call root constructor
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _sObjectConstructor);

            foreach (var method in _methods)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, _siDbConnectionCreateCommand);
                il.Emit(OpCodes.Stfld, method.CmdField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, method.CmdField);
                il.Emit(OpCodes.Ldstr, method.Sql);
                il.Emit(OpCodes.Callvirt, _siDbCommandSetCommandText);

                if (method.Parms.Length <= 0) continue;

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, method.CmdField);
                il.Emit(OpCodes.Callvirt, _siDbCommandGetParameters);
                il.Emit(OpCodes.Stloc_0);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, method.Parms.Length);
                il.Emit(OpCodes.Newarr, typeof(IDbDataParameter));
                il.Emit(OpCodes.Stfld, method.PrsField);


                for (var i = 0; i < method.Parms.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, method.PrsField);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, method.CmdField);
                    il.Emit(OpCodes.Callvirt, _siDbCommandCreateParameter);
                    il.Emit(OpCodes.Stelem_Ref);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, method.PrsField);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    var pattrs = method.Parms[i].GetCustomAttributes(typeof(ParameterMapAttribute), true);
                    il.Emit(OpCodes.Ldstr,
                        pattrs.Length != 0 ? ((ParameterMapAttribute)pattrs[0]).Param : "@" + method.Parms[i].Name);
                    il.Emit(OpCodes.Callvirt, _siDataParameterSetParameterName);

                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, method.PrsField);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Callvirt, _siListAdd);
                    il.Emit(OpCodes.Pop);
                }
            }
            il.Emit(OpCodes.Ret);
        }

        void CompileMethod(MethodCompileInfo method, MethodBuilder nMethod, ModuleBuilder mb)
        {
            var siDbCommandExecuteNonQuery = typeof(IDbCommand).GetMethod("ExecuteNonQuery");
            var siDbCommandExecuteScalar = typeof(IDbCommand).GetMethod("ExecuteScalar");
            var siDbCommandExecuteReader = typeof(IDbCommand).GetMethod("ExecuteReader", new Type[0]);

            var il = nMethod.GetILGenerator();
            var i = 0;
            foreach (var parm in method.Parms)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, method.PrsField);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                il.Emit(OpCodes.Ldarg, i + 1);
                if (parm.ParameterType.IsValueType) il.Emit(OpCodes.Box, parm.ParameterType);
                il.Emit(OpCodes.Callvirt, _siDataParameterSetValue);

                i++;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, method.CmdField);

            if(method.RetEnumType!=null)
            {
                var enumeratorT = EnumeratorCompiler.Compile(method.RetEnumType, mb);
                il.Emit(OpCodes.Callvirt, siDbCommandExecuteReader);
                il.Emit(OpCodes.Newobj, enumeratorT.GetConstructor(new[]{typeof(IDataReader)}));
                il.Emit(OpCodes.Ret);
            }
            else
            {
                switch (method.Infos.ReturnType.Name)
                {
                    case "Void":
                        il.Emit(OpCodes.Callvirt, siDbCommandExecuteNonQuery);
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ret);
                        break;
                    case "Object":
                        il.Emit(OpCodes.Callvirt, siDbCommandExecuteScalar);
                        il.Emit(OpCodes.Ret);
                        break;
                    case "IDataReader":
                        il.Emit(OpCodes.Callvirt, siDbCommandExecuteReader);
                        il.Emit(OpCodes.Ret);
                        break;

                    default:
                        throw new ArgumentException("Method " + method.Infos.Name + " returns incompatible type " + method.Infos.ReturnType.Name);
                }

            }
        }

        private void IssueMethods(TypeBuilder cType, ModuleBuilder mb)
        {
            foreach (var method in _methods)
            {
                var paramsT = new List<Type>(method.Parms.Length);
                for (var u = 0; u < method.Parms.Length; u++) paramsT.Add(method.Parms[u].ParameterType);

                var nMethod = cType.DefineMethod(method.Infos.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                    method.Infos.ReturnType, paramsT.ToArray());

                CompileMethod(method, nMethod,mb);
            }
        }


        static public T CreateInstance<T>(IDbConnection cnx) where T : class
        {
            var t = typeof(T);
            if (!t.IsInterface) throw new ArgumentException("T must be an interface");

            if (TypeCache.ContainsKey(t.FullName))
            {
                return (T)TypeCache[t.FullName].GetConstructor(new[] { typeof(IDbConnection) }).Invoke(new object[] { cnx });
            }


            var compiler = new BridgeCompiler(t);

            var anme = new AssemblyName("DbBridge_" + DateTime.Now.Ticks);
            var dynAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(anme, AssemblyBuilderAccess.RunAndSave, "C:\\");
            var module = dynAssembly.DefineDynamicModule("DbBridgeModule_" + DateTime.Now.Ticks, "DbBridge.module");
            var cType = module.DefineType("DbBridge_" + t.Name,
                TypeAttributes.Class | TypeAttributes.Public, null, new[] { t });

            compiler.EmitMemberVars(cType);
            compiler.EmitConstructor(cType);
            compiler.IssueMethods(cType, module);

            var retType = cType.CreateType();
            TypeCache.Add(t.FullName, retType);

            //dynAssembly.Save("__TEST.DLL");
            //return null;

            return (T)retType.GetConstructor(new[] { typeof(IDbConnection) }).Invoke(new object[] { cnx });
        }
    }
}
