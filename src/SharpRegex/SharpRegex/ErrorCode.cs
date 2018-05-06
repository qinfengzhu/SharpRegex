using System;

namespace SharpRegex
{
    /// <summary>
    /// 正则表达式错误信息
    /// </summary>
    public enum ErrorCode
    {
        Err_Success, //正确的正则表达式模式
        Err_Pren_Mismatch,    //"(A(D*)"
        Err_Empty_Pren,       //"()"
        Err_Empty_Bracket,    //"[]"
        Err_Bracket_Mismatch, //"["
        Err_Operand_Missing,  //"A|"
        Err_Invalid_Escape,   //"\A"
        Err_Invalid_Range,    //"[C-A]"
        Err_Empty_String      //""
    }
}
