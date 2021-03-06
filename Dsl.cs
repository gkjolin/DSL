﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dsl
{
    public delegate void DslLogDelegation(string msg);
    internal enum DslBinaryCode
    {
        BeginStatement = 1,
        EndStatement,
        BeginFunction,
        EndFunction,
        BeginCall,
        EndCall,
        BeginValue,
        EndValue,
        BeginExternScript,
        EndExternScript,
        ValueTypeBegin,
        ValueTypeEnd = ValueTypeBegin + ValueData.MAX_TYPE,
        ParamTypeBegin
    }
    /// <summary>
    /// 基于函数样式的脚本化数据解析工具。可以用作DSL元语言。
    /// </summary>
    /// <remarks>
    /// 混淆的当前实现要求脚本里不能出现`字符。另外，测试代码的加解密表的设计要求脚本里不能出现以@开头的标识符。
    /// </remarks>
    public interface ISyntaxComponent
    {
        bool IsValid();
        string GetId();
        int GetIdType();
        int GetLine();
        string ToScriptString();
    }

    public abstract class AbstractSyntaxCompoent : ISyntaxComponent
    {
        public const int ID_TOKEN = 0;
        public const int NUM_TOKEN = 1;
        public const int STRING_TOKEN = 2;
        public const int BOOL_TOKEN = 3;
        public const int MAX_TYPE = 3;

        public abstract bool IsValid();
        public abstract string GetId();
        public abstract int GetIdType();
        public abstract int GetLine();
        public abstract string ToScriptString();
    }
    /// <summary>
    /// 空语法单件
    /// </summary>
    public class NullSyntax : AbstractSyntaxCompoent
    {
        public override bool IsValid()
        {
            return false;
        }
        public override string GetId()
        {
            return "";
        }
        public override int GetIdType()
        {
            return ID_TOKEN;
        }
        public override int GetLine()
        {
            return -1;
        }
        public override string ToScriptString()
        {
            return ToString();
        }

        public static NullSyntax Instance
        {
            get
            {
                return s_Instance;
            }
        }
        private static NullSyntax s_Instance = new NullSyntax();
    }
    /// <summary>
    /// 用于描述变量、常量与无参命令语句。可能会出现在函数调用参数表与函数语句列表中。
    /// </summary>
    public class ValueData : AbstractSyntaxCompoent
    {
        public override bool IsValid()
        {
            return HaveId();
        }
        public override string GetId()
        {
            return m_Id;
        }
        public override int GetIdType()
        {
            return m_Type;
        }
        public override int GetLine()
        {
            return m_Line;
        }
        public override string ToScriptString()
        {
#if FULL_VERSION
            return Utility.quoteString(m_Id, m_Type);
#else
      return ToString();
#endif
        }

        public bool HaveId()
        {
            return !string.IsNullOrEmpty(m_Id) || m_Type == STRING_TOKEN;
        }
        public void SetId(string id)
        {
            m_Id = id;
        }
        public void SetType(int _type)
        {
            m_Type = _type;
        }
        public void SetLine(int line)
        {
            m_Line = line;
        }
        public bool IsId()
        {
            return ID_TOKEN == m_Type;
        }
        public bool IsNumber()
        {
            return NUM_TOKEN == m_Type;
        }
        public bool IsString()
        {
            return STRING_TOKEN == m_Type;
        }
        public bool IsBoolean()
        {
            return BOOL_TOKEN == m_Type;
        }
        public void Clear()
        {
            m_Type = ID_TOKEN;
            m_Id = "";
            m_Line = -1;
        }
        public void CopyFrom(ValueData other)
        {
            m_Type = other.m_Type;
            m_Id = other.m_Id;
            m_Line = other.m_Line;
        }

        public ValueData()
        { }
        public ValueData(string val)
        {
            if (val == "true" || val == "false")
                m_Type = BOOL_TOKEN;
            else if (Utility.needQuote(val))
                m_Type = STRING_TOKEN;
            else if (val.Length > 0 && (val[0] >= '0' && val[0] <= '9' || val[0] == '.' || val[0] == '-'))
                m_Type = NUM_TOKEN;
            else
                m_Type = ID_TOKEN;

            m_Id = val;
            m_Line = -1;
        }
        public ValueData(string val, int _type)
        {
            m_Type = _type;
            m_Id = val;
            m_Line = -1;
        }

        private int m_Type = ID_TOKEN;
        private string m_Id = "";
        private int m_Line = -1;
    }
    /// <summary>
    /// 函数调用数据，可能出现在函数头、参数表与函数语句列表中。
    /// </summary>
    public class CallData : AbstractSyntaxCompoent
    {
        public enum ParamClassEnum
        {
            PARAM_CLASS_MIN = 0,
            PARAM_CLASS_NOTHING = PARAM_CLASS_MIN,
            PARAM_CLASS_PARENTHESIS,
            PARAM_CLASS_BRACKET,
            PARAM_CLASS_PERIOD,
            PARAM_CLASS_PERIOD_PARENTHESIS,
            PARAM_CLASS_PERIOD_BRACKET,
            PARAM_CLASS_PERIOD_BRACE,
            PARAM_CLASS_OPERATOR,
            PARAM_CLASS_TERNARY_OPERATOR,
            PARAM_CLASS_MAX,
            PARAM_CLASS_WRAP_OBJECT_MEMBER_ASSIGN_MASK = 0x80,
            PARAM_CLASS_UNMASK = 0x7F,
        }
        public override bool IsValid()
        {
            return HaveId() || HaveParam();
        }
        public override string GetId()
        {
            if (null != m_Name)
                return m_Name.GetId();
            else if (null != m_Call)
                return m_Call.GetId();
            else
                return "";
        }
        public override int GetIdType()
        {
            if (null != m_Name)
                return m_Name.GetIdType();
            else if (null != m_Call)
                return m_Call.GetIdType();
            else
                return ID_TOKEN;
        }
        public override int GetLine()
        {
            if (null != m_Name)
                return m_Name.GetLine();
            else if (null != m_Call)
                return m_Call.GetLine();
            else
                return -1;
        }
        public override string ToScriptString()
        {
#if FULL_VERSION
            return Utility.getCallString(this);
#else
      return ToString();
#endif
        }

        public List<ISyntaxComponent> Params
        {
            get
            {
                return m_Params;
            }
            set
            {
                m_Params = value;
                if (value.Count > 0) {
                    if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                        m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
                    }
                }
            }
        }
        public bool IsHighOrder
        {
            get { return m_IsHighOrder; }
        }
        public ValueData Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                m_Call = null;
                m_IsHighOrder = false;
            }
        }
        public CallData Call
        {
            get { return m_Call; }
            set
            {
                m_Name = null;
                m_Call = value;
                m_IsHighOrder = true;
            }
        }
        public bool HaveId()
        {
            if (null != m_Name)
                return m_Name.HaveId();
            else if (null != m_Call)
                return m_Call.HaveId();
            else
                return false;
        }
        public void SetParamClass(int type)
        {
            int innerType = type & (int)ParamClassEnum.PARAM_CLASS_UNMASK;
            if (innerType >= (int)ParamClassEnum.PARAM_CLASS_MIN && innerType < (int)ParamClassEnum.PARAM_CLASS_MAX) {
                m_ParamClass = type;
            }
        }
        public int GetParamClass()
        {
            return m_ParamClass;
        }
        public bool HaveParam()
        {
            return (int)ParamClassEnum.PARAM_CLASS_NOTHING != m_ParamClass;
        }
        public int GetParamNum()
        {
            return m_Params.Count;
        }
        public void SetParam(int index, ISyntaxComponent data)
        {
            if (index < 0 || index >= m_Params.Count)
                return;
            m_Params[index] = data;
        }
        public ISyntaxComponent GetParam(int index)
        {
            if (index < 0 || index >= (int)m_Params.Count)
                return null;
            return m_Params[index];
        }
        public string GetParamId(int index)
        {
            if (index < 0 || index >= (int)m_Params.Count)
                return "";
            return m_Params[index].GetId();
        }
        public void ClearParams()
        {
            m_Params.Clear();
        }
        public void AddParams(string id)
        {
            m_Params.Add(new ValueData(id));
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(string id, int type)
        {
            m_Params.Add(new ValueData(id, type));
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(ValueData param)
        {
            m_Params.Add(param);
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(CallData param)
        {
            m_Params.Add(param);
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(FunctionData param)
        {
            m_Params.Add(param);
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(StatementData param)
        {
            m_Params.Add(param);
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void AddParams(ISyntaxComponent param)
        {
            m_Params.Add(param);
            if ((int)ParamClassEnum.PARAM_CLASS_NOTHING == m_ParamClass) {
                m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_PARENTHESIS;
            }
        }
        public void Clear()
        {
            m_Name = null;
            m_Call = null;
            m_IsHighOrder = false;
            m_Params = new List<ISyntaxComponent>();
            m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_NOTHING;
        }
        public void CopyFrom(CallData other)
        {
            m_IsHighOrder = other.m_IsHighOrder;
            m_Name = other.m_Name;
            m_Call = other.m_Call;
            m_Params = other.m_Params;
            m_ParamClass = other.m_ParamClass;
        }

        private bool m_IsHighOrder = false;
        private ValueData m_Name = null;
        private CallData m_Call = null;
        private List<ISyntaxComponent> m_Params = new List<ISyntaxComponent>();
        private int m_ParamClass = (int)ParamClassEnum.PARAM_CLASS_NOTHING;
    }
    /// <summary>
    /// 函数数据，由函数调用数据+语句列表构成。
    /// </summary>
    public class FunctionData : AbstractSyntaxCompoent
    {
        public enum ExtentClassEnum
        {
            EXTENT_CLASS_MIN = 0,
            EXTENT_CLASS_NOTHING = EXTENT_CLASS_MIN,
            EXTENT_CLASS_STATEMENT,
            EXTENT_CLASS_EXTERN_SCRIPT,
            EXTENT_CLASS_MAX,
        };
        public override bool IsValid()
        {
            return HaveId() || HaveParam() || HaveStatement() || HaveExternScript();
        }
        public override string GetId()
        {
            if (null != m_Call)
                return m_Call.GetId();
            else
                return "";
        }
        public override int GetIdType()
        {
            if (null != m_Call)
                return m_Call.GetIdType();
            else
                return ID_TOKEN;
        }
        public override int GetLine()
        {
            if (null != m_Call)
                return m_Call.GetLine();
            else
                return -1;
        }
        public override string ToScriptString()
        {
#if FULL_VERSION
            //与write方法不同，这里输出无缩进单行表示
            string line = "";
            if (null != m_Call)
                line = m_Call.ToScriptString();
            StringBuilder stream = new StringBuilder();
            stream.Append(line);
            if (HaveStatement()) {
                stream.Append("{");
                int ct = GetStatementNum();
                for (int i = 0; i < ct; ++i) {
                    ISyntaxComponent data = GetStatement(i);
                    stream.Append(data.ToScriptString());
                    stream.Append(";");
                }
                stream.Append("}");
            } else if (HaveExternScript()) {
                stream.Append("{:");
                stream.Append(GetExternScript());
                stream.Append(":}");
            }
            return stream.ToString();
#else
      return ToString();
#endif
        }

        public CallData Call
        {
            get { return m_Call; }
            set { m_Call = value; }
        }
        public List<ISyntaxComponent> Statements
        {
            get
            {
                return m_Statements;
            }
            set
            {
                m_Statements = value;
                if (value.Count > 0) {
                    if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                        m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
                    }
                }
            }
        }
        public void SetExtentClass(int type)
        {
            if (type >= (int)ExtentClassEnum.EXTENT_CLASS_MIN && type < (int)ExtentClassEnum.EXTENT_CLASS_MAX) {
                m_ExtentClass = type;
            }
        }
        public int GetExtentClass()
        {
            return m_ExtentClass;
        }
        public bool HaveId()
        {
            if (null != m_Call)
                return m_Call.HaveId();
            else
                return false;
        }
        public bool HaveParam()
        {
            if (null != m_Call)
                return m_Call.HaveParam();
            else
                return false;
        }
        public bool HaveStatement()
        {
            return (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT == m_ExtentClass;
        }
        public bool HaveExternScript()
        {
            return (int)ExtentClassEnum.EXTENT_CLASS_EXTERN_SCRIPT == m_ExtentClass;
        }
        public void SetExternScript(string scp)
        {
            m_ExternScript = scp;
        }
        public string GetExternScript()
        {
            return m_ExternScript;
        }
        public int GetStatementNum()
        {
            return m_Statements.Count;
        }
        public void SetStatement(int index, ISyntaxComponent data)
        {
            if (index < 0 || index >= m_Statements.Count)
                return;
            m_Statements[index] = data;
        }
        public ISyntaxComponent GetStatement(int index)
        {
            if (index < 0 || index >= m_Statements.Count)
                return NullSyntax.Instance;
            return m_Statements[index];
        }
        public string GetStatementId(int index)
        {
            if (index < 0 || index >= m_Statements.Count)
                return "";
            return m_Statements[index].GetId();
        }
        public void ClearStatements()
        {
            m_Statements.Clear();
        }
        public void AddStatement(string id)
        {
            m_Statements.Add(new ValueData(id));
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(string id, int type)
        {
            m_Statements.Add(new ValueData(id, type));
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(ValueData statement)
        {
            m_Statements.Add(statement);
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(CallData statement)
        {
            m_Statements.Add(statement);
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(FunctionData statement)
        {
            m_Statements.Add(statement);
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(StatementData statement)
        {
            m_Statements.Add(statement);
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void AddStatement(ISyntaxComponent statement)
        {
            m_Statements.Add(statement);
            if ((int)ExtentClassEnum.EXTENT_CLASS_STATEMENT != m_ExtentClass) {
                m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_STATEMENT;
            }
        }
        public void Clear()
        {
            m_Call = null;
            m_Statements = new List<ISyntaxComponent>();
            m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_NOTHING;
            m_ExternScript = null;
        }
        public void CopyFrom(FunctionData other)
        {
            m_Call = other.m_Call;
            m_Statements = other.m_Statements;
            m_ExtentClass = other.m_ExtentClass;
            m_ExternScript = other.m_ExternScript;
        }

        private CallData m_Call = null;
        private List<ISyntaxComponent> m_Statements = new List<ISyntaxComponent>();
        private int m_ExtentClass = (int)ExtentClassEnum.EXTENT_CLASS_NOTHING;
        private string m_ExternScript = null;

        public static FunctionData NullFunctionData
        {
            get
            {
                return s_NullFunctionData;
            }
        }
        private static FunctionData s_NullFunctionData = new FunctionData();
    }
    /// <summary>
    /// 语句数据，由多个函数数据连接而成。
    /// </summary>
    public class StatementData : AbstractSyntaxCompoent
    {
        public override bool IsValid()
        {
            bool ret = true;
            if (m_Functions.Count <= 0) {
                ret = false;
            } else {
                for (int i = 0; i < m_Functions.Count; ++i) {
                    ret = ret && m_Functions[i].IsValid();
                }
            }
            return ret;
        }
        public override string GetId()
        {
            if (m_Functions.Count <= 0)
                return "";
            else
                return m_Functions[0].GetId();
        }
        public override int GetIdType()
        {
            if (m_Functions.Count <= 0)
                return ID_TOKEN;
            else
                return m_Functions[0].GetIdType();
        }
        public override int GetLine()
        {
            if (m_Functions.Count <= 0)
                return -1;
            else
                return m_Functions[0].GetLine();
        }
        public override string ToScriptString()
        {
#if FULL_VERSION
            //与write方法不同，这里输出无缩进单行表示
            FunctionData tempData = First;
            CallData callData = null;
            callData = tempData.Call;
            if (null != callData && callData.GetParamClass() == (int)CallData.ParamClassEnum.PARAM_CLASS_TERNARY_OPERATOR) {
                if (callData.HaveId() && callData.HaveParam() && tempData.HaveStatement()) {
                    string line = string.Format("{0} {1} {2}", callData.GetParam(0).ToScriptString(), callData.GetId(), tempData.GetStatement(0).ToScriptString());
                    if (Functions.Count == 2) {
                        FunctionData funcData = Functions[1];
                        if (funcData.HaveId() && funcData.HaveStatement())
                            line = string.Format("{0} {1} {2}", line, funcData.GetId(), funcData.GetStatement(0).ToScriptString());
                    }
                    return line;
                } else {
                    return "";
                }
            } else {
                StringBuilder stream = new StringBuilder();
                int ct = Functions.Count;
                for (int i = 0; i < ct; ++i) {
                    FunctionData funcData = Functions[i];
                    stream.Append(funcData.ToScriptString());
                }
                return stream.ToString();
            }
#else
      return ToString();
#endif
        }

        public List<FunctionData> Functions
        {
            get { return m_Functions; }
        }
        public FunctionData First
        {
            get
            {
                if (m_Functions.Count > 0)
                    return m_Functions[0];
                else
                    return FunctionData.NullFunctionData;
            }
        }
        public FunctionData Second
        {
            get
            {
                if (m_Functions.Count > 1)
                    return m_Functions[1];
                else
                    return FunctionData.NullFunctionData;
            }
        }
        public FunctionData Last
        {
            get
            {
                if (m_Functions.Count > 0)
                    return m_Functions[m_Functions.Count - 1];
                else
                    return FunctionData.NullFunctionData;
            }
        }
        public void Clear()
        {
            m_Functions = new List<FunctionData>();
        }
        public void CopyFrom(StatementData other)
        {
            m_Functions = other.m_Functions;
        }

        private List<FunctionData> m_Functions = new List<FunctionData>();

        public static StatementData NullStatementData
        {
            get
            {
                return s_NullStatementData;
            }
        }
        private static StatementData s_NullStatementData = new StatementData();
    }

    public class DslInfo : StatementData
    {
        public override string ToScriptString()
        {
            StringBuilder stream = new StringBuilder();
            Utility.writeStatementData(stream, this, 0);
            return stream.ToString();
        }

        public void SetLoaded(bool loaded)
        {
            mLoaded = loaded;
        }
        public bool IsLoaded()
        {
            return mLoaded;
        }
        public void SetResourceName(string name)
        {
            mResourceName = name;
        }
        public string GetResourceName()
        {
            return mResourceName;
        }
        public new void Clear()
        {
            base.Clear();
            mLoaded = false;
            mResourceName = "";
        }
        public void CopyFrom(DslInfo other)
        {
            base.CopyFrom((StatementData)other);
            mLoaded = other.mLoaded;
            mResourceName = other.mResourceName;
        }

        private bool mLoaded = false;
        private string mResourceName = "";
    };
    public class DslFile
    {
        public List<DslInfo> DslInfos
        {
            get { return mDslInfos; }
        }
        public void AddDslInfo(DslInfo data)
        {
            mDslInfos.Add(data);
        }

        public bool Load(string file, DslLogDelegation logCallback)
        {
#if FULL_VERSION
            string content = File.ReadAllText(file);
            //logCallback(string.Format("DslFile.Load {0}:\n{1}", file, content));
            return LoadFromString(content, file, logCallback);
#else
      return false;
#endif
        }
        public bool LoadFromString(string content, string resourceName, DslLogDelegation logCallback)
        {
#if FULL_VERSION
            mDslInfos.Clear();
            Parser.DslLog log = new Parser.DslLog();
            log.OnLog += logCallback;
            Parser.DslToken tokens = new Parser.DslToken(log, content);
            Parser.DslError error = new Parser.DslError(log, tokens);
            Parser.RuntimeAction action = new Parser.RuntimeAction(mDslInfos);
            action.onGetLastToken = () => { return tokens.getLastToken(); };
            action.onGetLastLineNumber = () => { return tokens.getLastLineNumber(); };
            action.onSetStringDelimiter = (string delimiter) => { tokens.setStringDelimiter(delimiter); };
            action.onSetScriptDelimiter = (string delimiter) => { tokens.setScriptDelimiter(delimiter); };

            Parser.DslParser.parse(action, tokens, error, 0);
            if (error.HasError) {
                for (int i = 0; i < mDslInfos.Count; i++) {
                    mDslInfos[i].Clear();
                }
            } else {
                for (int i = 0; i < mDslInfos.Count; i++) {
                    mDslInfos[i].SetResourceName(resourceName);
                }
            }
            return !error.HasError;
#else
      return false;
#endif
        }
        public void Save(string file)
        {
#if FULL_VERSION
            using (StreamWriter sw = new StreamWriter(file)) {
                if (null != sw) {
                    for (int i = 0; i < mDslInfos.Count; i++) {
                        sw.Write(mDslInfos[i].ToScriptString());
                        sw.Write("\r\n");
                    }
                    sw.Close();
                }
            }
#endif
        }

        public string GenerateBinaryCode(string content, Dictionary<string, string> encodeTable, DslLogDelegation logCallback)
        {
#if FULL_VERSION
            List<DslInfo> infos = new List<DslInfo>();
            Parser.DslLog log = new Parser.DslLog();
            log.OnLog += logCallback;
            Parser.DslToken tokens = new Parser.DslToken(log, content);
            Parser.DslError error = new Parser.DslError(log, tokens);
            Parser.RuntimeAction action = new Parser.RuntimeAction(infos);
            action.onGetLastToken = () => { return tokens.getLastToken(); };
            action.onGetLastLineNumber = () => { return tokens.getLastLineNumber(); };
            action.onSetStringDelimiter = (string delimiter) => { tokens.setStringDelimiter(delimiter); };
            action.onSetScriptDelimiter = (string delimiter) => { tokens.setScriptDelimiter(delimiter); };
            Parser.DslParser.parse(action, tokens, error, 0);
            if (error.HasError) {
                return string.Empty;
            } else {
                MemoryStream stream = new MemoryStream();
                List<string> identifiers = new List<string>();
                foreach (DslInfo info in infos) {
                    Utility.writeBinary(stream, identifiers, (StatementData)info);
                }

                byte[] bytes = stream.ToArray();
                SortedDictionary<string, int> dict = new SortedDictionary<string, int>();
                int ct = identifiers.Count;
                if (ct > 0x00004000) {
                    System.Diagnostics.Debug.Assert(false);
                    //Console.WriteLine("Identifiers count {0} too large than 0x04000", ct);
                    return string.Empty;
                }
                for (int i = 0; i < ct; ++i) {
                    string key = identifiers[i];
                    key = Encode(key, encodeTable);
                    identifiers[i] = key;
                    if (!dict.ContainsKey(key)) {
                        dict.Add(key, 0);
                    }
                }
                List<string> keys = new List<string>(dict.Keys);
                byte[] bytes2;
                using (MemoryStream ms = new MemoryStream()) {
                    for (int i = 0; i < ct; ++i) {
                        string key = identifiers[i];
                        int ix = keys.BinarySearch(key);
                        if (ix < 0x80) {
                            ms.WriteByte((byte)ix);
                        } else {
                            ms.WriteByte((byte)((ix & 0x0000007f) | 0x00000080));
                            ms.WriteByte((byte)(ix >> 7));
                        }
                    }
                    bytes2 = ms.ToArray();
                }
                return string.Format("{0}|{1}|{2}", Convert.ToBase64String(bytes), Convert.ToBase64String(bytes2), string.Join("`", keys.ToArray()));
            }
#else
      return string.Empty;
#endif
        }
        public void LoadBinaryFile(string file, Dictionary<string, string> decodeTable)
        {
            string code = File.ReadAllText(file);
            LoadBinaryCode(code, decodeTable);
        }
        public void LoadBinaryCode(string binaryCode, Dictionary<string, string> decodeTable)
        {
            mDslInfos.Clear();
            if (string.IsNullOrEmpty(binaryCode))
                return;
            int split = binaryCode.IndexOf('|');
            if (split <= 0)
                return;
            string code = binaryCode.Substring(0, split);
            string left = binaryCode.Substring(split + 1);
            int split2 = left.IndexOf('|');
            if (split2 <= 0)
                return;
            string code2 = left.Substring(0, split2);
            byte[] bytes = Convert.FromBase64String(code);
            byte[] bytes2 = Convert.FromBase64String(code2);
            string[] keys = left.Substring(split2 + 1).Split('`');
            if (null != bytes && null != bytes2) {
                List<string> ids = new List<string>();
                for (int i = 0; i < bytes2.Length; ++i) {
                    int ix;
                    byte first = bytes2[i];
                    if ((first & 0x80) == 0x80) {
                        ++i;
                        byte second = bytes2[i];
                        ix = (int)(((int)first & 0x0000007f) | ((int)second << 7));
                    } else {
                        ix = first;
                    }
                    if (ix >= 0 && ix < keys.Length) {
                        ids.Add(Decode(keys[ix], decodeTable));
                    } else {
                        ids.Add(string.Empty);
                    }
                }
                List<DslInfo> infos = Utility.readBinary(bytes, ids);
                mDslInfos.AddRange(infos);
            }
        }

        private string Encode(string s0, Dictionary<string, string> encodeTable)
        {
#if FULL_VERSION
            string s;
            if (!encodeTable.TryGetValue(s0, out s))
                s = s0;
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            s = Convert.ToBase64String(bytes);
            int len = s.Length;
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < len; ++i) {
                char c = (char)((char)158 - s[i]);
                if (c != '`')
                    str.Append(c);
                else
                    str.Append(s[i]);
            }
            return str.ToString();
#else
      return "";
#endif
        }
        private string Decode(string s, Dictionary<string, string> decodeTable)
        {
            int len = s.Length;
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < len; ++i) {
                char c = (char)((char)158 - s[i]);
                if (c != '`')
                    str.Append(c);
                else
                    str.Append(s[i]);
            }
            s = str.ToString();
            byte[] bytes = Convert.FromBase64String(s);
            s = Encoding.UTF8.GetString(bytes);
            string s0;
            if (!decodeTable.TryGetValue(s, out s0))
                s0 = s;
            return s0;
        }

        private List<DslInfo> mDslInfos = new List<DslInfo>();
    };

    public sealed class Utility
    {
        public static bool needQuote(string str)
        {
            const string escapeChars = " \t\r\n{}()[],;~`!%^&*-+=|:<>?/#\\";
            if (str.Length == 0) {
                return true;
            }
            bool haveDot = false;
            bool notNum = false;
            for (int i = 0; i < str.Length; ++i) {
                char c = str[i];
                if (escapeChars.IndexOf(c) >= 0) {
                    return true;
                }
                if (c == '.') {
                    haveDot = true;
                } else if (!notNum && !char.IsDigit(c)) {
                    notNum = true;
                }
                if (haveDot && notNum) {
                    return true;
                }
            }
            return false;
        }

        public static string quoteString(string str, int _Type)
        {
            switch (_Type) {
                case AbstractSyntaxCompoent.STRING_TOKEN: {
                    if (str.IndexOf('\n') >= 0)
                        return "\"\r\n" + str + "\"";
                    else
                        return "\"" + str + "\"";
                    }
                case AbstractSyntaxCompoent.NUM_TOKEN:
                case AbstractSyntaxCompoent.BOOL_TOKEN:
                    return str;
            }
            if (needQuote(str)) {
                if (str.IndexOf('\n') >= 0)
                    return "\"\r\n" + str + "\"";
                else
                    return "\"" + str + "\"";
            }
            return str;
        }

        public static string unquoteString(string str)
        {
            int len = str.Length;
            if (len > 2 && (str[0] == '\"' && str[len - 1] == '\"' || str[0] == '\'' && str[len - 1] == '\'') && !needQuote(str)) {
                return str.Substring(1, len - 2);
            }
            return str;
        }

        public static string getCallString(CallData data)
        {
#if FULL_VERSION
            string lineNo = "";//formatString("/* %d */",data.GetLine());
            string line = "";
            if (data.IsHighOrder) {
                line = getCallString(data.Call);
            } else if (data.HaveId()) {
                line = quoteString(data.GetId(), data.GetIdType());
            }
            if (data.HaveParam()) {
                int paramClass = (data.GetParamClass() & (int)CallData.ParamClassEnum.PARAM_CLASS_UNMASK);
                if ((int)CallData.ParamClassEnum.PARAM_CLASS_OPERATOR == paramClass) {
                    switch (data.GetParamNum()) {
                        case 1:
                            return string.Format("{0} {1}", line, data.GetParam(0).ToScriptString());
                        case 2:
                            return string.Format("{0} {1} {2}", data.GetParam(0).ToScriptString(), line, data.GetParam(1).ToScriptString());
                        default:
                            return line;
                    }
                } else {
                    string lbracket = "";
                    string rbracket = "";
                    switch (paramClass) {
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_PARENTHESIS:
                            lbracket = "(";
                            rbracket = ")";
                            break;
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_BRACKET:
                            lbracket = "[";
                            rbracket = "]";
                            break;
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_PERIOD:
                            lbracket = ".";
                            rbracket = "";
                            break;
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_PERIOD_PARENTHESIS:
                            lbracket = ".(";
                            rbracket = ")";
                            break;
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_PERIOD_BRACKET:
                            lbracket = ".[";
                            rbracket = "]";
                            break;
                        case (int)CallData.ParamClassEnum.PARAM_CLASS_PERIOD_BRACE:
                            lbracket = ".{";
                            rbracket = "}";
                            break;
                    }
                    StringBuilder stream = new StringBuilder();
                    stream.Append(lbracket);
                    int ct = data.GetParamNum();
                    for (int i = 0; i < ct; ++i) {
                        if (i > 0)
                            stream.Append(",");
                        ISyntaxComponent param = data.GetParam(i);
                        if ((int)CallData.ParamClassEnum.PARAM_CLASS_PERIOD == paramClass)
                            stream.Append(unquoteString(param.ToScriptString()));
                        else
                            stream.Append(param.ToScriptString());
                    }
                    stream.Append(rbracket);
                    return string.Format("{0}{1}{2}", lineNo, line, stream.ToString());
                }
            } else {
                return string.Format("{0}{1}", lineNo, line);
            }
#else
      return "";
#endif
        }

        public static void writeContent(StringBuilder stream, string line, int indent)
        {
#if FULL_VERSION
            for (int i = 0; i < indent; ++i) {
                stream.Append('\t');
            }
            stream.Append(line);
#endif
        }

        public static void writeLine(StringBuilder stream, string line, int indent)
        {
#if FULL_VERSION
            writeContent(stream, line, indent);
            stream.Append("\r\n");
#endif
        }

        public static void writeSyntaxComponent(StringBuilder stream, ISyntaxComponent data, int indent, bool isLastOfStatement)
        {
#if FULL_VERSION
            ValueData val = data as ValueData;
            if (null != val) {
                if (isLastOfStatement)
                    writeLine(stream, val.ToScriptString() + ";", indent);
                else
                    writeLine(stream, val.ToScriptString(), indent);
            } else {
                CallData call = data as CallData;
                if (null != call) {
                    if (isLastOfStatement)
                        writeLine(stream, call.ToScriptString() + ";", indent);
                    else
                        writeLine(stream, call.ToScriptString(), indent);
                } else {
                    FunctionData function = data as FunctionData;
                    if (null != function) {
                        writeFunctionData(stream, function, indent, isLastOfStatement);
                    } else {
                        StatementData statement = data as StatementData;
                        writeStatementData(stream, statement, indent);
                    }
                }
            }
#endif
        }

        public static void writeFunctionData(StringBuilder stream, FunctionData data, int indent, bool isLastOfStatement)
        {
#if FULL_VERSION
            string line = "";
            if (null != data.Call)
                line = data.Call.ToScriptString();
            if (data.HaveStatement()) {
                if (line.Length > 0) {
                    writeLine(stream, line, indent);
                }
                writeLine(stream, "{", indent);
                ++indent;

                int ct = data.GetStatementNum();
                for (int i = 0; i < ct; ++i) {
                    ISyntaxComponent tempData = data.GetStatement(i);
                    writeSyntaxComponent(stream, tempData, indent, true);
                }

                --indent;
                if (isLastOfStatement)
                    writeLine(stream, "};", indent);
                else
                    writeLine(stream, "}", indent);
            } else if (data.HaveExternScript()) {
                if (line.Length > 0) {
                    writeLine(stream, line, indent);
                }
                string script = data.GetExternScript();
                if (script.IndexOf('\n') >= 0) {
                    writeLine(stream, "{:", indent);
                } else {
                    writeContent(stream, "{:", indent);
                }
                stream.Append(script);
                if (script.Length>0 && script[script.Length - 1] == '\n') {
                    if (isLastOfStatement)
                        writeLine(stream, ":};", indent);
                    else
                        writeLine(stream, ":}", indent);
                } else {
                    if (isLastOfStatement)
                        stream.AppendLine(":};");
                    else
                        stream.AppendLine(":}");
                }
            } else {
                if (isLastOfStatement)
                    writeLine(stream, line + ";", indent);
                else
                    writeLine(stream, line, indent);
            }
#endif
        }

        public static void writeStatementData(StringBuilder stream, StatementData data, int indent)
        {
#if FULL_VERSION
            FunctionData tempData = data.First;
            CallData callData = tempData.Call;
            if (null != callData && callData.GetParamClass() == (int)CallData.ParamClassEnum.PARAM_CLASS_TERNARY_OPERATOR) {
                if (callData.HaveId() && callData.HaveParam() && tempData.HaveStatement()) {
                    string line = string.Format("{0} {1} {2}", callData.GetParam(0).ToScriptString(), callData.GetId(), tempData.GetStatement(0).ToScriptString());
                    if (data.Functions.Count == 2) {
                        FunctionData funcData = data.Functions[1];
                        if (funcData.HaveId() && funcData.HaveStatement())
                            line = string.Format("{0} {1} {2}", line, funcData.GetId(), funcData.GetStatement(0).ToScriptString());
                    }
                    writeLine(stream, line + ";", indent);
                }
            } else {
                int ct = data.Functions.Count;
                bool isLastOfStatement = false;
                if (ct == 0)
                    isLastOfStatement = true;
                for (int i = 0; i < ct; ++i) {
                    if (i == ct - 1)
                        isLastOfStatement = true;
                    FunctionData func = data.Functions[i];
                    writeFunctionData(stream, func, indent, isLastOfStatement);
                }
            }
#endif
        }

        private static byte readByte(byte[] bytes, int curCodeIndex)
        {
            if (curCodeIndex < bytes.Length)
                return bytes[curCodeIndex];
            else
                return 0;
        }
        private static string readIdentifier(List<string> identifiers, int curIdIndex)
        {
            if (curIdIndex < identifiers.Count)
                return identifiers[curIdIndex];
            else
                return string.Empty;
        }
        internal static List<DslInfo> readBinary(byte[] bytes, List<string> identifiers)
        {
            List<DslInfo> infos = new List<DslInfo>();
            int curCodeIndex = 0;
            int curIdIndex = 0;
            while (curCodeIndex < bytes.Length) {
                while (curCodeIndex < bytes.Length && bytes[curCodeIndex] != (byte)DslBinaryCode.BeginStatement)
                    ++curCodeIndex;
                if (curCodeIndex < bytes.Length) {
                    DslInfo info = new DslInfo();
                    readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, (StatementData)info);
                    if (info.IsValid()) {
                        info.SetLoaded(true);
                        infos.Add(info);
                    }
                }
            }
            return infos;
        }
        internal static ISyntaxComponent readBinary(byte[] bytes, ref int curCodeIndex, List<string> identifiers, ref int curIdIndex)
        {
            ISyntaxComponent ret = null;
            byte code = readByte(bytes, curCodeIndex);
            if (code == (byte)DslBinaryCode.BeginValue) {
                ValueData data = new ValueData();
                readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, data);
                ret = data;
            } else if (code == (byte)DslBinaryCode.BeginCall) {
                CallData data = new CallData();
                readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, data);
                ret = data;
            } else if (code == (byte)DslBinaryCode.BeginFunction) {
                FunctionData data = new FunctionData();
                readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, data);
                ret = data;
            } else if (code == (byte)DslBinaryCode.BeginStatement) {
                StatementData data = new StatementData();
                readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, data);
                ret = data;
            }
            return ret;
        }
        internal static void readBinary(byte[] bytes, ref int curCodeIndex, List<string> identifiers, ref int curIdIndex, ValueData data)
        {
            byte code = readByte(bytes, curCodeIndex++);
            if (code == (byte)DslBinaryCode.BeginValue) {
                code = readByte(bytes, curCodeIndex);
                if (code >= (byte)DslBinaryCode.ValueTypeBegin) {
                    ++curCodeIndex;
                    data.SetType(code - (byte)DslBinaryCode.ValueTypeBegin);
                    data.SetId(readIdentifier(identifiers, curIdIndex++));
                }
                code = readByte(bytes, curCodeIndex);
                if (code == (byte)DslBinaryCode.EndValue) {
                    ++curCodeIndex;
                }
            }
        }
        internal static void readBinary(byte[] bytes, ref int curCodeIndex, List<string> identifiers, ref int curIdIndex, CallData data)
        {
            byte code = readByte(bytes, curCodeIndex++);
            if (code == (byte)DslBinaryCode.BeginCall) {
                code = readByte(bytes, curCodeIndex);
                if (code >= (byte)DslBinaryCode.ParamTypeBegin) {
                    ++curCodeIndex;
                    data.SetParamClass(code - (byte)DslBinaryCode.ParamTypeBegin);
                }
                code = readByte(bytes, curCodeIndex);
                if (code == (byte)DslBinaryCode.BeginValue) {
                    ValueData valueData = new ValueData();
                    readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, valueData);
                    data.Name = valueData;
                } else if (code == (byte)DslBinaryCode.BeginCall) {
                    CallData callData = new CallData();
                    readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, callData);
                    data.Call = callData;
                }
                for (; ; ) {
                    code = readByte(bytes, curCodeIndex);
                    if (code == (byte)DslBinaryCode.EndCall) {
                        ++curCodeIndex;
                        break;
                    } else {
                        ISyntaxComponent syntaxData = readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex);
                        if (null != syntaxData) {
                            data.Params.Add(syntaxData);
                        } else {
                            break;
                        }
                    }
                }
            }
        }
        internal static void readBinary(byte[] bytes, ref int curCodeIndex, List<string> identifiers, ref int curIdIndex, FunctionData data)
        {
            byte code = readByte(bytes, curCodeIndex++);
            if (code == (byte)DslBinaryCode.BeginFunction) {
                code = readByte(bytes, curCodeIndex);
                if (code == (byte)DslBinaryCode.BeginCall) {
                    CallData callData = new CallData();
                    readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, callData);
                    data.Call = callData;
                }
                code = readByte(bytes, curCodeIndex);
                if (code == (byte)DslBinaryCode.BeginExternScript) {
                    ++curCodeIndex;
                    data.SetExtentClass((int)FunctionData.ExtentClassEnum.EXTENT_CLASS_EXTERN_SCRIPT);
                    data.SetExternScript(readIdentifier(identifiers, curIdIndex++));

                    code = readByte(bytes, curCodeIndex);
                    if (code == (byte)DslBinaryCode.EndExternScript) {
                        ++curCodeIndex;
                    }
                } else {
                    data.SetExtentClass((int)FunctionData.ExtentClassEnum.EXTENT_CLASS_STATEMENT);
                    for (; ; ) {
                        code = readByte(bytes, curCodeIndex);
                        if (code == (byte)DslBinaryCode.EndFunction) {
                            ++curCodeIndex;
                            break;
                        } else {
                            ISyntaxComponent syntaxData = readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex);
                            if (null != syntaxData) {
                                data.Statements.Add(syntaxData);
                            } else {
                                break;
                            }
                        }
                    }
                }
            }
        }
        internal static void readBinary(byte[] bytes, ref int curCodeIndex, List<string> identifiers, ref int curIdIndex, StatementData data)
        {
            byte code = readByte(bytes, curCodeIndex++);
            if (code == (byte)DslBinaryCode.BeginStatement) {
                for (; ; ) {
                    code = readByte(bytes, curCodeIndex);
                    if (code == (byte)DslBinaryCode.BeginFunction) {
                        FunctionData funcData = new FunctionData();
                        readBinary(bytes, ref curCodeIndex, identifiers, ref curIdIndex, funcData);
                        data.Functions.Add(funcData);
                    } else if (code == (byte)DslBinaryCode.EndStatement) {
                        ++curCodeIndex;
                        break;
                    } else {
                        break;
                    }
                }
            }
        }
        //---------------------------------------------------------------------------------------------
#if FULL_VERSION
        internal static void writeBinary(MemoryStream stream, List<string> identifiers, ISyntaxComponent data)
        {
            ValueData val = data as ValueData;
            if (null != val) {
                writeBinary(stream, identifiers, val);
            } else {
                CallData call = data as CallData;
                if (null != call) {
                    writeBinary(stream, identifiers, call);
                } else {
                    FunctionData function = data as FunctionData;
                    if (null != function) {
                        writeBinary(stream, identifiers, function);
                    } else {
                        StatementData statement = data as StatementData;
                        writeBinary(stream, identifiers, statement);
                    }
                }
            }
        }
        internal static void writeBinary(MemoryStream stream, List<string> identifiers, ValueData data)
        {
            stream.WriteByte((byte)DslBinaryCode.BeginValue);
            if (null != data) {
                stream.WriteByte((byte)((int)DslBinaryCode.ValueTypeBegin + data.GetIdType()));
                identifiers.Add(data.GetId());
            }
            stream.WriteByte((byte)DslBinaryCode.EndValue);
        }
        internal static void writeBinary(MemoryStream stream, List<string> identifiers, CallData data)
        {
            stream.WriteByte((byte)DslBinaryCode.BeginCall);
            if (null != data) {
                stream.WriteByte((byte)((int)DslBinaryCode.ParamTypeBegin + data.GetParamClass()));
                if (data.IsHighOrder) {
                    writeBinary(stream, identifiers, data.Call);
                } else {
                    writeBinary(stream, identifiers, data.Name);
                }
                foreach (ISyntaxComponent syntaxData in data.Params) {
                    writeBinary(stream, identifiers, syntaxData);
                }
            }
            stream.WriteByte((byte)DslBinaryCode.EndCall);
        }
        internal static void writeBinary(MemoryStream stream, List<string> identifiers, FunctionData data)
        {
            stream.WriteByte((byte)DslBinaryCode.BeginFunction);
            writeBinary(stream, identifiers, data.Call);
            if (data.HaveExternScript()) {
                stream.WriteByte((byte)DslBinaryCode.BeginExternScript);
                identifiers.Add(data.GetExternScript());
                stream.WriteByte((byte)DslBinaryCode.EndExternScript);
            } else {
                foreach (ISyntaxComponent syntaxData in data.Statements) {
                    writeBinary(stream, identifiers, syntaxData);
                }
            }
            stream.WriteByte((byte)DslBinaryCode.EndFunction);
        }
        internal static void writeBinary(MemoryStream stream, List<string> identifiers, StatementData data)
        {
            stream.WriteByte((byte)DslBinaryCode.BeginStatement);
            foreach (FunctionData funcData in data.Functions) {
                writeBinary(stream, identifiers, funcData);
            }
            stream.WriteByte((byte)DslBinaryCode.EndStatement);
        }
#endif
    }
}
