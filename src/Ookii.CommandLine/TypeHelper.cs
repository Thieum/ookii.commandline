﻿// Copyright (c) Sven Groot (Ookii.org)
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy
// of the license should be distributed with the code.  It can also be found
// at https://github.com/SvenGroot/ookii.commandline. This notice, the author's name,
// and all copyright notices must remain intact in all applications,
// documentation, and source files.
using System;

namespace Ookii.CommandLine
{
    static class TypeHelper
    {
        public static Type FindGenericInterface(Type type, Type interfaceType)
        {
            if( type == null )
                throw new ArgumentNullException("type");
            if( interfaceType == null )
                throw new ArgumentNullException("interfaceType");
            if( !(interfaceType.IsInterface && interfaceType.IsGenericTypeDefinition) )
                throw new ArgumentException(Properties.Resources.TypeNotGenericDefinition, "interfaceType");

            if( type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType )
                return type;

            foreach( Type t in type.GetInterfaces() )
            {
                if( t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType )
                    return t;
            }

            return null;
        }
    }
}
