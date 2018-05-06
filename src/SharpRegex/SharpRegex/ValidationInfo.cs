using System;

namespace SharpRegex
{
    /// <summary>
    /// 验证信息
    /// </summary>
    internal class ValidationInfo
    {
        public ErrorCode ErrorCode = ErrorCode.Err_Success;
        public int ErrorStartAt = -1;
        public int ErrorLength = -1;
        public string FormattedString = string.Empty;
        public bool MatchAtStart = false;
        public bool MatchAtEnd = false;
    }
}
