using System;



namespace LiteDB
{
    public class ConnectionManager
    {
        private static ConnectionManager _instance;
        private static readonly object _lock = new object();
        public ConnectionString ConnectionString;
        private ConnectionManager()
        {
        }

        public static ConnectionManager GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ConnectionManager();
                    }
                }
            }
            return _instance;
        }
    }

  
}
