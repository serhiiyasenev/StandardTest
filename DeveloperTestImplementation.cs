#region Copyright statement
// --------------------------------------------------------------
// Copyright (C) 1999-2016 Exclaimer Ltd. All Rights Reserved.
// No part of this source file may be copied and/or distributed 
// without the express permission of a director of Exclaimer Ltd
// ---------------------------------------------------------------
#endregion
using DeveloperTestInterfaces;
using System;
using System.IO;

namespace DeveloperTest
{
    public sealed class DeveloperTestImplementation : IDeveloperTest
    {
        public void RunQuestionOne(ICharacterReader reader, IOutputResult output)
        {
            try
            {

            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine("End of stream");
            }

            output.AddResult(reader.GetNextChar().ToString());
        }

        public void RunQuestionTwo(ICharacterReader[] readers, IOutputResult output)
        {
            throw new NotImplementedException();
        }
    }
}