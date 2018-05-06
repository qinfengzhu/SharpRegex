using System;
using System.Text;

namespace SharpRegex
{
    /// <summary>
    /// 验证正则表达式模式: 使用递归下降分析进行验证
    /// 除了验证模式之外,它还有两个其他任务:1.插入隐式令牌和扩展Charecter类
    /// 
    /// 正则的原子操作分为3种
    /// X|Y  并 Alternate
    /// XY   链接 Concatenation
    /// X*   克林闭包 KleeneStar
    /// 
    /// "AB"---------->"A.B"  插入拼接符号
    /// "A.B"--------->"A\.B" 插入转义符号
    /// "[A-C]"------->"(A|B|C)" Range转换为合并
    /// "(AB"--------->错配错误报告
    /// </summary>
    internal class RegExValidator
    {
        private bool m_bConcante = false;
        private bool m_bAlternate = false;

        private const char m_chNull = '\0';//null symbol;
        private char m_chSym = m_chNull;

        private int m_nPatternLength = -1;
        private string m_sPattern = string.Empty;
        private int m_nCurrPos = -1;
        private StringBuilder m_sb = null;

        private ValidationInfo m_validationInfo = null;

        public RegExValidator() { }

        public ValidationInfo Validate(string sPattern)
        {
            m_chSym = m_chNull;
            m_bConcante = false;
            m_bAlternate = false;
            m_sb = new StringBuilder(1024);
            m_nCurrPos = -1;
            m_sPattern = sPattern;
            m_nPatternLength = m_sPattern.Length;

            m_validationInfo = new ValidationInfo();

            if(sPattern.Length==0)
            {
                m_validationInfo.ErrorCode = ErrorCode.Err_Empty_String;
                return m_validationInfo;
            }

            GetNextSymbol();

            string sLit1 = MetaSymbol.MatchStart.ToString();
            string sLit2 = MetaSymbol.MatchEnd.ToString();
            string sLit3 = sLit1 + sLit2;

            if(!(sPattern.CompareTo(sLit1)==0||sPattern.CompareTo(sLit2)==0||sPattern.CompareTo(sLit3)==0))
            {
                if(sPattern[0]==MetaSymbol.MatchStart)
                {
                    m_validationInfo.MatchAtStart = true;
                    Accept(MetaSymbol.MatchStart);
                }
                if(m_sPattern[m_nPatternLength-1]==MetaSymbol.MatchEnd)
                {
                    m_validationInfo.MatchAtEnd = true;
                    m_nPatternLength--;
                }
            }

            try
            {
                while(m_nCurrPos<m_nPatternLength)
                {
                    switch(m_chSym)
                    {
                        case MetaSymbol.Alternate:
                        case MetaSymbol.OneOrMore:
                        case MetaSymbol.ZeroOrMore:
                        case MetaSymbol.ZeroOrOne:
                            Abort(ErrorCode.Err_Operand_Missing, m_nCurrPos, 1);
                            break;
                        case MetaSymbol.ClosePren:
                            Abort(ErrorCode.Err_Pren_Mismatch, m_nCurrPos, 1);
                            break;
                        default:
                            Expression();
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            m_validationInfo.FormattedString = m_sb.ToString();
            return m_validationInfo;
        }

        /// <summary>
        /// 获取下一个字符
        /// m_nCurrPos++
        /// </summary>
        private void GetNextSymbol()
        {
            m_nCurrPos++;
            if(m_nCurrPos<m_nPatternLength)
            {
                m_chSym = m_sPattern[m_nCurrPos];
            }
            else
            {
                m_chSym = m_chNull;
            }
        }
        /// <summary>
        /// 接受一个指定的字符,并且下移到下一个字符
        /// </summary>
        /// <param name="ch">可接受的字符</param>
        /// <returns></returns>
        private bool Accept(char ch)
        {
            if(m_chSym==ch)
            {
                GetNextSymbol();
                return true;
            }
            return false;
        }
        /// <summary>
        /// 记录错误位置,并且抛出异常
        /// </summary>
        /// <param name="errCode">错误代码</param>
        /// <param name="nErrPosition">错误位置</param>
        /// <param name="nErrLen">错误长度</param>
        private void Abort(ErrorCode errCode,int nErrPosition,int nErrLen)
        {
            m_validationInfo.ErrorCode = errCode;
            m_validationInfo.ErrorStartAt = nErrPosition;
            m_validationInfo.ErrorLength = nErrLen;

            throw new Exception("Syntex error.");
        }
        /// <summary>
        /// 表达式正式转换工作
        /// </summary>
        private void Expression()
        {
            //转义符
            while(Accept(MetaSymbol.Escape))
            {
                AppendConate();
                if(!ExpectEscapeChar())
                {
                    Abort(ErrorCode.Err_Invalid_Escape, m_nCurrPos - 1, 1);
                }
                AcceptPostfixOperator();
                m_bConcante = true;
            }
            //链接
            while(Accept(MetaSymbol.Concanate))
            {
                AppendConate();
                m_sb.Append(MetaSymbol.Escape);
                m_sb.Append(MetaSymbol.Concanate);
                AcceptPostfixOperator();
                m_bConcante = true;
            }
            //起始符号
            while(Accept(MetaSymbol.Complement))
            {
                AppendConate();
                m_sb.Append(MetaSymbol.Escape);
                m_sb.Append(MetaSymbol.Complement);
                AcceptPostfixOperator();
                m_bConcante = true;
            }
            //非转义字符
            while(AcceptNoEscapeChar())
            {
                AcceptPostfixOperator();
                m_bConcante = true;
                Expression();
            }
            //接受分组字符
            if(Accept(MetaSymbol.OpenPren))
            {
                int nEntryPos = m_nCurrPos - 1;
                AppendConate();
                m_sb.Append(MetaSymbol.OpenPren);
                Expression();
                if (!Expect(MetaSymbol.OpenPren))
                {
                    Abort(ErrorCode.Err_Pren_Mismatch, nEntryPos, m_nCurrPos - nEntryPos);
                }
                m_sb.Append(MetaSymbol.ClosePren);

                int nLen = m_nCurrPos - nEntryPos;
                if (nLen == 2)
                {
                    Abort(ErrorCode.Err_Empty_Pren, nEntryPos, m_nCurrPos - nEntryPos);
                }

                AcceptPostfixOperator();
                m_bConcante = true;
                Expression();
            }
            //接受范围开始字符
            if(Accept(MetaSymbol.CharSetStart))
            {
                int nEntryPos = m_nCurrPos - 1;
                bool bComplement = false;

                AppendConate();

                if (Accept(MetaSymbol.Complement))
                {
                    bComplement = true;
                }

                string sTmp = m_sb.ToString();

                m_sb = new StringBuilder(1024);
                m_bAlternate = false;
                CharecterSet();

                if (!Expect(MetaSymbol.CharSetEnd))
                {
                    Abort(ErrorCode.Err_Bracket_Mismatch, nEntryPos, m_nCurrPos - nEntryPos);
                }

                int nLen = m_nCurrPos - nEntryPos;

                if (nLen == 2)  // "[]"
                {
                    Abort(ErrorCode.Err_Empty_Bracket, nEntryPos, m_nCurrPos - nEntryPos);
                }
                else if (nLen == 3 && bComplement == true) // "[^]"  - treat the complement as literal
                {
                    m_sb = new StringBuilder(1024);
                    m_sb.Append(sTmp);
                    m_sb.Append(MetaSymbol.OpenPren);
                    m_sb.Append(MetaSymbol.Escape);
                    m_sb.Append(MetaSymbol.Complement);
                    m_sb.Append(MetaSymbol.ClosePren);
                }
                else
                {
                    string sCharset = m_sb.ToString();
                    m_sb = new StringBuilder(1024);
                    m_sb.Append(sTmp);
                    if (bComplement)
                    {
                        m_sb.Append(MetaSymbol.Complement);
                    }
                    m_sb.Append(MetaSymbol.OpenPren);
                    m_sb.Append(sCharset /*ExpandRange(sCharset, nEntryPos) */   );
                    m_sb.Append(MetaSymbol.ClosePren);
                }

                AcceptPostfixOperator();

                m_bConcante = true;

                Expression();
            }
            //接受并运算字符
            if(Accept(MetaSymbol.Alternate))
            {
                int nEntryPos = m_nCurrPos - 1;
                m_bConcante = false;
                m_sb.Append(MetaSymbol.Alternate);
                Expression();
                int nLen = m_nCurrPos - nEntryPos;
                if (nLen == 1)
                {
                    Abort(ErrorCode.Err_Operand_Missing, nEntryPos, m_nCurrPos - nEntryPos);
                }
                Expression();
            }
        }
        /// <summary>
        /// 接受一个链接 . 
        /// </summary>
        private void AppendConate()
        {
            if(m_bConcante)
            {
                m_sb.Append(MetaSymbol.Concanate);
                m_bConcante = false;
            }
        }
        /// <summary>
        /// 接受一个并 |
        /// </summary>
        private void AppendAlternate()
        {
            if(m_bAlternate)
            {
                m_sb.Append(MetaSymbol.Alternate);
                m_bAlternate = false;
            }
        }
        private bool ExpectEscapeChar()
        {
            switch(m_chSym)
            {
                case MetaSymbol.Alternate:
                case MetaSymbol.AnyOneChar:
                case MetaSymbol.CharSetStart:
                case MetaSymbol.ClosePren:
                case MetaSymbol.Complement:
                case MetaSymbol.Escape:
                case MetaSymbol.OneOrMore:
                case MetaSymbol.OpenPren:
                case MetaSymbol.ZeroOrMore:
                case MetaSymbol.ZeroOrOne:
                    m_sb.Append(MetaSymbol.Escape);
                    m_sb.Append(m_chSym);
                    Accept(m_chSym);
                    break;
                case MetaSymbol.NewLine:
                    m_sb.Append('\n');
                    Accept(m_chSym);
                    break;
                case MetaSymbol.Tab:
                    m_sb.Append('\t');
                    Accept(m_chSym);
                    break;
                default:
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 接受后缀操作符号
        /// </summary>
        /// <returns></returns>
        private bool AcceptPostfixOperator()
        {
            switch(m_chSym)
            {
                case MetaSymbol.OneOrMore:
                case MetaSymbol.ZeroOrMore:
                case MetaSymbol.ZeroOrOne:
                    m_sb.Append(m_chSym);
                    return Accept(m_chSym);
                default:
                    return false;
            }
        }
        /// <summary>
        /// 接受非转义字符
        /// </summary>
        /// <returns></returns>
        private bool AcceptNoEscapeChar()
        {
            switch(m_chSym)
            {
                case MetaSymbol.Alternate:
                case MetaSymbol.CharSetStart:
                case MetaSymbol.ClosePren:
                case MetaSymbol.Escape:
                case MetaSymbol.OneOrMore:
                case MetaSymbol.OpenPren:
                case MetaSymbol.ZeroOrMore:
                case MetaSymbol.ZeroOrOne:
                case MetaSymbol.Concanate:
                case m_chNull:
                    return false;
                default:
                    AppendConate();
                    m_sb.Append(m_chSym);
                    Accept(m_chSym);
                    break;
            }
            return true;
        }
        private bool Expect(char ch)
        {
            if(Accept(ch))
            {
                return true;
            }
            return false;
        }
        private void CharecterSet()
        {
            int nRangeFormStartAt = -1;
            int nStartAt = -1;
            int nLength = -1;

            // xx-xx form
            string sLeft = String.Empty;
            string sRange = String.Empty;
            string sRight = String.Empty;


            string sTmp = String.Empty;

            while (true)
            {
                sTmp = String.Empty;

                nStartAt = m_nCurrPos;

                if (Accept(MetaSymbol.Escape))
                {
                    if ((sTmp = ExpectEscapeCharInBracket()) == String.Empty)
                    {
                        Abort(ErrorCode.Err_Invalid_Escape, m_nCurrPos - 1, 1);
                    }
                    nLength = 2;
                }

                if (sTmp == String.Empty)
                {
                    sTmp = AcceptNonEscapeCharInBracket();
                    nLength = 1;
                }

                if (sTmp == String.Empty)
                {
                    break;
                }

                if (sLeft == String.Empty)
                {
                    nRangeFormStartAt = nStartAt;
                    sLeft = sTmp;
                    AppendAlternate();
                    m_sb.Append(sTmp);
                    m_bAlternate = true;
                    continue;
                }

                if (sRange == String.Empty)
                {
                    if (sTmp != MetaSymbol.Range.ToString())
                    {
                        nRangeFormStartAt = nStartAt;
                        sLeft = sTmp;
                        AppendAlternate();
                        m_sb.Append(sTmp);
                        m_bAlternate = true;
                        continue;
                    }
                    else
                    {
                        sRange = sTmp;
                    }
                    continue;
                }

                sRight = sTmp;


                bool bOk = ExpandRange(sLeft, sRight);

                if (bOk == false)
                {
                    int nSubstringLen = (nStartAt + nLength) - nRangeFormStartAt;

                    Abort(ErrorCode.Err_Invalid_Range, nRangeFormStartAt, nSubstringLen);
                }
                sLeft = String.Empty;
                sRange = String.Empty;
                sRange = String.Empty;
            }

            if (sRange != String.Empty)
            {
                AppendAlternate();
                m_sb.Append(sRange);
                m_bAlternate = true;
            }

        }
        private bool ExpandRange(string sLeft, string sRight)
        {
            char chLeft = (sLeft.Length > 1 ? sLeft[1] : sLeft[0]);
            char chRight = (sRight.Length > 1 ? sRight[1] : sRight[0]);

            if (chLeft > chRight)
            {
                return false;
            }

            chLeft++;
            while (chLeft <= chRight)
            {
                AppendAlternate();

                switch (chLeft)
                {
                    case MetaSymbol.Alternate:
                    case MetaSymbol.AnyOneChar:
                    case MetaSymbol.ClosePren:
                    case MetaSymbol.Complement:
                    case MetaSymbol.Concanate:
                    case MetaSymbol.Escape:
                    case MetaSymbol.OneOrMore:
                    case MetaSymbol.ZeroOrMore:
                    case MetaSymbol.ZeroOrOne:
                    case MetaSymbol.OpenPren:
                        m_sb.Append(MetaSymbol.Escape);
                        break;
                    default:
                        break;
                }

                m_sb.Append(chLeft);
                m_bAlternate = true;
                chLeft++;
            }
            return true;
        }
        private string ExpectEscapeCharInBracket()
        {
            char ch = m_chSym;

            switch (m_chSym)
            {
                case MetaSymbol.CharSetEnd:
                case MetaSymbol.Escape:
                    Accept(m_chSym);
                    return MetaSymbol.Escape.ToString() + ch.ToString();
                case MetaSymbol.NewLine:
                    Accept(m_chSym);
                    return ('\n').ToString();
                case MetaSymbol.Tab:
                    Accept(m_chSym);
                    return ('\t').ToString();
                default:
                    return String.Empty;
            }
        }
        private string AcceptNonEscapeCharInBracket()
        {
            char ch = m_chSym;

            switch (ch)
            {
                case MetaSymbol.CharSetEnd:
                case MetaSymbol.Escape:
                case m_chNull:
                    return String.Empty;
                case MetaSymbol.Alternate:
                case MetaSymbol.AnyOneChar:
                case MetaSymbol.ClosePren:
                case MetaSymbol.Complement:
                case MetaSymbol.OneOrMore:
                case MetaSymbol.OpenPren:
                case MetaSymbol.ZeroOrMore:
                case MetaSymbol.ZeroOrOne:
                case MetaSymbol.Concanate:
                    Accept(m_chSym);
                    return MetaSymbol.Escape.ToString() + ch.ToString();
                default:
                    Accept(m_chSym);
                    return ch.ToString();
            }
        }
    }
}
