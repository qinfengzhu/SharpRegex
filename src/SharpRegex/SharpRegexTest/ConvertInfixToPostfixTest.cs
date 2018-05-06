using System;
using NUnit.Framework;
using System.Text;
using System.Collections;
using SharpRegex;
using System.Collections.Generic;

namespace SharpRegexTest
{
    /// <summary>
    /// 正则表达式中缀表示转换为后缀表示
    /// </summary>
    [TestFixture]
    public class ConvertInfixToPostfixTest
    {
        private string ConvertToPostfix(string infixPattern)
        {
            Stack<char> stackOperator = new Stack<char>();
            Queue<char> queuePostfix = new Queue<char>();

            bool hasEscape = false;//队列是否有转义字符

            for(int i=0,l=infixPattern.Length;i<l;i++)
            {
                char currentChar = infixPattern[i];

                if(hasEscape==false&&currentChar==MetaSymbol.Escape)
                {
                    queuePostfix.Enqueue(currentChar);
                    hasEscape = true;
                    continue;
                }
                if(hasEscape==true)
                {
                    queuePostfix.Enqueue(currentChar);
                    hasEscape = false;
                    continue;
                }

                switch(currentChar)
                {
                    case MetaSymbol.OpenPren:
                        stackOperator.Push(currentChar);
                        break;
                    case MetaSymbol.ClosePren:
                        while((char)stackOperator.Peek()!=MetaSymbol.OpenPren)
                        {
                            queuePostfix.Enqueue(stackOperator.Pop());
                        }
                        stackOperator.Pop();//pop the '('
                        break;
                    default:
                        while(stackOperator.Count>0)
                        {
                            char charPeeked = stackOperator.Peek();
                            int nPriorityPeek = GetOperatorPriority(charPeeked);
                            int nPriorityCurr = GetOperatorPriority(currentChar);

                            if(nPriorityPeek>=nPriorityCurr)
                            {
                                queuePostfix.Enqueue(stackOperator.Pop());
                            }
                            else
                            {
                                break;
                            }
                        }
                        stackOperator.Push(currentChar);
                        break;
                }
            }//end of for ..loop

            while(stackOperator.Count>0)
            {
                queuePostfix.Enqueue(stackOperator.Pop());
            }
            StringBuilder sb = new StringBuilder(1024);
            while(queuePostfix.Count>0)
            {
                sb.Append(queuePostfix.Dequeue());
            }
            return sb.ToString();
        }
        private int GetOperatorPriority(char charOpt)
        {
            switch (charOpt)
            {
                case MetaSymbol.OpenPren:
                    return 0;
                case MetaSymbol.Alternate:
                    return 1;
                case MetaSymbol.Concanate:
                    return 2;
                case MetaSymbol.ZeroOrOne:
                case MetaSymbol.ZeroOrMore:
                case MetaSymbol.OneOrMore:
                    return 3;
                case MetaSymbol.Complement:
                    return 4;
                default:
                    return 5;
            }
        }
    }
}
