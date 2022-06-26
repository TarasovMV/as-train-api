using System;
namespace VrRestApi.Services
{
    public class AdditionalService
    {
        public string GenerateCode(int length)
        {
            Random rnd = new Random();
            string code = "";
            for (int i = 0; i < length; i++)
            {
                int random = rnd.Next(0, 9);
                code += random.ToString();
            }
            return code;
        }

        public string CodeResult()
        {
            return "";
        }

        public int[] DecodeResult(string code, int questionsCount = 2)
        {
            // code = concat(9 - (true answer)) * 5
            try
            {
                int[] res = new int[questionsCount];
                if (code?.Length < questionsCount)
                {
                    return null;
                }

                code = (int.Parse(code) / 5).ToString();
                if (code.Length < questionsCount)
                {
                    return null;
                }
                for (int i = 0; i < questionsCount; i++)
                {
                    res[i] = 9 - int.Parse(code[i].ToString());
                }
                return res;
            } catch
            {
                return null;
            }
            
        }
    }
}
