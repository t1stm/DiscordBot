using System;
using System.Collections.Generic;

#nullable enable

namespace DiscordBot.Abstract
{
    public enum Status
    {
        Ok,
        Fail
    }

    public enum Empty
    {
        Placeholder
    }
    
    public class Result <T, U>
    {
        protected readonly T Ok;
        protected readonly U Fail;
        protected readonly Status Status;

        public Result(T ok, U fail, Status status)
        {
            Ok = ok;
            Fail = fail;
            Status = status;
        }

        public static Result<T, Empty> Passed(T a)
        {
            return new(a, Empty.Placeholder, Status.Ok);
        }

        public static Result<Empty, U> Failed(U b)
        {
            return new(Empty.Placeholder, b, Status.Fail);
        }

        public static bool operator ==(Result<T,U> yes, Status s)
        {
            return yes.Status == s;
        }

        public static bool operator !=(Result<T, U> yes, Status s)
        {
            return !(yes == s);
        }
        
        protected bool Equals(Result<T, U> other)
        {
            return EqualityComparer<T>.Default.Equals(Ok, other.Ok) && 
                   EqualityComparer<U>.Default.Equals(Fail, other.Fail) && 
                   Status == other.Status;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Result<T, U>) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ok, Fail, (int) Status);
        }
    }
}