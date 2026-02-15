using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace UTS.Backend.Domain
{
    public sealed class UtsDocument
    {
        public string ClassName { get; set; } = "Unnamed";

        /// <summary>
        /// Number of tests per period
        /// </summary>
        public int T { get; set; }        // tests count

        /// <summary>
        /// Number of students graded each test
        /// </summary>
        public int K { get; set; }        // target graded per test
        
        /// <summary>
        /// Minimum number of tests graded at the end of the periond
        /// </summary>
        public int E { get; set; }        // minimum graded per student

        public ObservableCollection<StudentRecord> Students { get; } = new();

        public void Validate()
        {
            if (T <= 0) throw new InvalidOperationException("T must be > 0.");
            if (K < 0) throw new InvalidOperationException("K must be >= 0.");
            if (E < 0) throw new InvalidOperationException("E must be >= 0.");
            foreach (var s in Students)
                if (s.TestsCount != T)
                    throw new InvalidOperationException("Student row length != T.");
        }
    }

}
