namespace ParseSharp
{
    public abstract class ParseLocatable
    {
        private int _lineNumber;
        private int _columnNumber;

        public int LineNumber
        {
            get
            {
                if (_lineNumber == 0) FindLocation();
                return _lineNumber;
            }
        }

        public int ColumnNumber
        {
            get
            {
                if (_columnNumber == 0) FindLocation();
                return _columnNumber;
            }
        }

        protected abstract string GetInput();
        protected abstract int GetOffset();

        private void FindLocation()
        {
            _lineNumber = 1;
            _columnNumber = 1;

            var input = GetInput();
            var offset = GetOffset();

            for (var i = 0; i < input.Length && i < offset; ++i) {
                switch (input[i]) {
                    case '\n':
                        ++_lineNumber;
                        _columnNumber = 1;
                        break;
                    default:
                        ++_columnNumber;
                        break;
                }
            }
        }
    }
}
