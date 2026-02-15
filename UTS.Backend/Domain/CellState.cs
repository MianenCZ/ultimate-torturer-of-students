using System;
using System.Collections.Generic;
using System.Text;

namespace UTS.Backend.Domain
{
    public enum CellState
    {
        None,       // '-'
        Absent,     // 'A'
        Recommended,// 'D'
        Graded,     // 'Z'
        Locked,     // 'L'
    }

}
