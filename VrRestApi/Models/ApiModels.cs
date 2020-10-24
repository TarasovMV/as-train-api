using System;
using System.Collections.Generic;

namespace VrRestApi.Models
{
    public class ApiTesting
    {
        public int? Id { get; set; }
        public string Title { get; set; }
    }

    public class ApiTestingQuestion
    {
        public int? Id { get; set; }
        public int TestingId { get; set; }
        public string Title { get; set; }
        public TestingPanoType? Pano { get; set; }
        public TestingQuestionType Type { get; set; }
        public List<ApiTestingAnswer> Answers { get; set; }
    }

    public class ApiTestingAnswer
    {
        public int? Id { get; set; }
        public int TestingQuestionId { get; set; }
        public string Title { get; set; }
        public bool IsValid { get; set; }
    }

    public class ApiHandler
    {
        public bool isActive { get; set; }
        public int? i { get; set; }
        public double? d { get; set; }
        public string title { get; set; }
    }

    public class JsonContainer<T>
    {
        public T data { get; set; }
        public JsonContainer(T obj)
        {
            data = obj;
        }
    }

    public static class ListExtra
    {
        private static Random rng = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

        public static void Resize<T>(this List<T> list, int size, T element = default(T))
        {
            if (size < list.Count)
                list.RemoveRange(size, list.Count - size);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
