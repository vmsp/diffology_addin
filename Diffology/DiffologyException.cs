using System;
using System.Data.OleDb;

namespace Diffology
{
    /// <summary>
    /// When an unrecoverable exception is thrown during sync, it is wrapped as a
    /// DiffologyException and thrown to the client code. It tries to convey as much
    /// useful information as possible in the message, to ease debugging.
    /// </summary>
    public class DiffologyException : Exception
    {
        internal DiffologyException(string message, Exception inner) : base(message, inner) { }

        internal DiffologyException(OleDbException inner) : base(OleDbMessageProvider(inner), inner) { }

        private static string OleDbMessageProvider(OleDbException e)
        {
            var errorMessages = "OleDbErrors List:\n";
            for (int i = 0; i < e.Errors.Count; i++)
            {
                errorMessages += "Index #" + i + "\n" +
                                 "Message: " + e.Errors[i].Message + "\n" +
                                 "NativeError: " + e.Errors[i].NativeError + "\n" +
                                 "Source: " + e.Errors[i].Source + "\n" +
                                 "SQLState: " + e.Errors[i].SQLState + "\n";
            }
            return errorMessages;
        }
    }
}
