using System.Collections.Generic;

namespace AngelDB
{
    public static class Tokenizer
    {
        public static List<string> Tokenize(string input, string startToken, string endToken)
        {
            List<string> tokens = new List<string>();
            int startIndex = 0;
            int endIndex;

            while ((startIndex = input.IndexOf(startToken, startIndex)) != -1)
            {
                if (startIndex > 0)
                {
                    string beforeToken = input.Substring(0, startIndex);
                    tokens.Add(beforeToken);
                    input = input.Substring(startIndex);
                    startIndex = 0;
                }

                startIndex += startToken.Length;
                endIndex = input.IndexOf(endToken, startIndex);

                if (endIndex == -1)
                {
                    break;
                }

                string token = "^" + input.Substring(startIndex, endIndex - startIndex);
                tokens.Add(token);

                input = input.Substring(endIndex + endToken.Length);
                startIndex = 0;
            }

            if (input.Length > 0)
            {
                tokens.Add(input);
            }

            return tokens;
        }
    }
}
