using System;
using System.Collections.Generic;
using System.Text;

namespace UTS.WPF.MVVM
{
    public class AboutModel
    {
        public string AssemblyVersion { get; } = typeof(AboutModel).Assembly.GetName().Version?.ToString() ?? "unknown";

        public string GitHubUrl { get; } = @"https://github.com/MianenCZ/ultimate-torturer-of-students";
    }
}
