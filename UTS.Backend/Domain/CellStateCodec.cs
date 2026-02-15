namespace UTS.Backend.Domain
{
    public static class CellStateCodec
    {
        public static char ToChar(CellState s) => s switch
        {
            CellState.Absent => 'A',
            CellState.Recommended => 'D',
            CellState.Graded => 'Z',
            _ => '-',
        };

        public static CellState FromChar(char c) => c switch
        {
            'A' => CellState.Absent,
            'D' => CellState.Recommended,
            'Z' => CellState.Graded,
            '-' => CellState.None,
            _ => throw new FormatException($"Invalid cell '{c}'. Expected A,D,Z,-"),
        };
    }

}
