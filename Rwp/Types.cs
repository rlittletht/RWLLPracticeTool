
using System;
using System.Collections.Generic;

namespace Rwp
{
    public class RSRBase
    {
        public bool Result { get; set; }
        public string Reason { get; set; }
        public bool Succeeded => Result;
    }

    public class RSR : RSRBase
    {
    }

    public class TRSR<T> : RSRBase
    {
        public T TheValue { get; set; }
    }

    public class ServerInfo
    {
        public string sSqlServerHash;
        public string sServerName;
    }

    public class CalItem
    {
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string UID { get; set; }
    }

    public class CalendarLink
    {
        public Guid Link { get; set; }
        public string Team { get; set; }
        public string Authority { get; set; }
        public DateTime CreateDate { get; set; }
        public string Comment { get; set; }
    }

    public class RSR_CalItems : TRSR<List<CalItem>>
    {
    }
}