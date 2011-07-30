﻿// $Id: CommandLineArgument.cs 40 2011-07-30 11:57:14Z sgroot $
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Ookii.CommandLine
{
    /// <summary>
    /// Provides information about command line arguments that are recognized by a <see cref="CommandLineParser"/>.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    public sealed class CommandLineArgument
    {
        private readonly CommandLineParser _parser;
        private readonly TypeConverter _converter;
        private readonly PropertyInfo _property;
        private readonly string _valueDescription;
        private readonly string _argumentName;
        private readonly Type _argumentType;
        private readonly string _description;
        private readonly object _defaultValue;
        private readonly bool _isRequired;
        private List<object> _arrayValues;
        private object _value;

        private CommandLineArgument(CommandLineParser parser, PropertyInfo property, string argumentName, Type argumentType, int? position, bool isRequired, object defaultValue, string description, string valueDescription)
        {
            // If this method throws anything other than a NotSupportedException, it constitutes a bug in the Ookii.CommandLine library.
            if( parser == null )
                throw new ArgumentNullException("parser");
            if( argumentName == null )
                throw new ArgumentNullException("argumentName");
            if( argumentType == null )
                throw new ArgumentNullException("argumentType");
            if( defaultValue != null && defaultValue.GetType() != argumentType )
                throw new NotSupportedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.IncorrectDefaultValueTypeFormat, argumentName));
            if( argumentName.Length == 0 )
                throw new NotSupportedException(Properties.Resources.EmptyArgumentName);

            _parser = parser;
            _property = property;
            _argumentName = argumentName;
            _argumentType = argumentType;
            _description = description;
            _defaultValue = defaultValue;
            _isRequired = isRequired;
            Position = position;
            _valueDescription = valueDescription ?? GetFriendlyTypeName(argumentType.IsArray ? argumentType.GetElementType() : argumentType);

            if( argumentType.IsArray )
            {
                if( argumentType.GetArrayRank() != 1 )
                    throw new ArgumentException(Properties.Resources.InvalidArrayRank, "argumentType");
                _converter = TypeDescriptor.GetConverter(argumentType.GetElementType());
            }
            else
                _converter = TypeDescriptor.GetConverter(argumentType);

            if( _converter == null || !_converter.CanConvertFrom(typeof(string)) )
                throw new NotSupportedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.NoTypeConverterFormat, argumentName, argumentType));
        }

        /// <summary>
        /// Gets the name of this argument.
        /// </summary>
        /// <value>
        /// The name of this argument.
        /// </value>
        /// <remarks>
        /// <para>
        ///   This name is used to supply an argument value by name on the command line, and to describe the argument in the usage help
        ///   generated by <see cref="CommandLineParser.WriteUsage(System.IO.TextWriter,int,WriteUsageOptions)"/>.
        /// </para>
        /// </remarks>
        public string ArgumentName
        {
            get { return _argumentName; }
        }

        /// <summary>
        /// Gets the type of the argument.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> of the argument.
        /// </value>
        public Type ArgumentType
        {
            get { return _argumentType; }
        }

        /// <summary>
        /// Gets the position of this argument.
        /// </summary>
        /// <value>
        /// The position of this argument, or <see langword="null"/> if this is not a positional argument.
        /// </value>
        /// <remarks>
        /// <para>
        ///   A positional argument is created either using a constructor parameter on the command line arguments type,
        ///   or by using the <see cref="CommandLineArgumentAttribute.Position"/> property to create a named
        ///   positional argument.
        /// </para>
        /// <para>
        ///   The <see cref="Position"/> property reflects the actual position of the positional argument. For positional
        ///   arguments created from properties this doesn't need to match the value of the <see cref="CommandLineArgumentAttribute.Position"/> property.
        /// </para>
        /// </remarks>
        public int? Position { get; internal set; }

        /// <summary>
        /// Gets a value that indicates whether the argument is required.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the argument's value must be specified on the command line; <see langword="false"/> if the argument may be omitted.
        /// </value>
        public bool IsRequired
        {
            get { return _isRequired; }
        }

        /// <summary>
        /// Gets the default value for an argument.
        /// </summary>
        /// <value>
        /// The default value of the argument.
        /// </value>
        /// <remarks>
        /// <para>
        ///   This value is only used if <see cref="IsRequired"/> is <see langword="false"/>.
        /// </para>
        /// </remarks>
        public object DefaultValue
        {
            get { return _defaultValue; }
        }

        /// <summary>
        /// Gets the description of the argument.
        /// </summary>
        /// <value>
        /// The description of the argument.
        /// </value>
        /// <remarks>
        /// <para>
        ///   This property is used only when generating usage information using <see cref="CommandLineParser.WriteUsage(System.IO.TextWriter,int,WriteUsageOptions)"/>.
        /// </para>
        /// <para>
        ///   To set the description of an argument, apply the <see cref="System.ComponentModel.DescriptionAttribute"/> attribute to the constructor parameter 
        ///   or the property that defines the argument.
        /// </para>
        /// </remarks>
        public string Description
        {
            get { return _description ?? ""; }
        }

        /// <summary>
        /// Gets the description of the property's value to use when printing usage information.
        /// </summary>
        /// <value>
        /// The description of the value.
        /// </value>
        /// <remarks>
        /// <para>
        ///   The value description is a short (typically one word) description that indicates the type of value that
        ///   the user should supply. By default the type of the property is used. If the type is an array type, the
        ///   array's element type is used. If the type is a nullable type, its underlying type is used.
        /// </para>
        /// <para>
        ///   The value description is used when printing usage. For example, the usage for an argument named Sample with
        ///   a value description of String would look like "/Sample &lt;String&gt;".
        /// </para>
        /// <note>
        ///   This is not the long description used to describe the purpose of the argument. That can be retrieved
        ///   using the <see cref="Description"/> property.
        /// </note>
        /// </remarks>
        public string ValueDescription
        {
            get { return _valueDescription; }
        }
        
        /// <summary>
        /// Gets a value indicating whether this argument is a switch argument.
        /// </summary>
        /// <value>
        /// 	<see langword="true"/> if the argument is a switch argument; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        ///   A switch argument is an argument that doesn't need a value; instead, its value is <see langword="true"/> or
        ///   <see langword="false"/> depending on whether the argument is present on the command line.
        /// </para>
        /// <para>
        ///   A argument is a switch argument when it is not positional, and its <see cref="CommandLineArgument.ArgumentType"/> is either <see cref="Boolean"/>, a nullable <see cref="Boolean"/> or an array of <see cref="Boolean"/>.
        /// </para>
        /// </remarks>
        public bool IsSwitch
        {
            get { return Position == null && (ArgumentType == typeof(bool) || ArgumentType == typeof(bool?) || ArgumentType == typeof(bool[])); }
        }

        /// <summary>
        /// Gets the value that the argument was set to in the last call to <see cref="CommandLineParser.Parse(string[],int)"/>.
        /// </summary>
        /// <value>
        ///   The value of the argument that was obtained when the command line arguments were parsed.
        /// </value>
        /// <remarks>
        /// <para>
        ///   The <see cref="Value"/> property provides an alternative method for accessing supplied argument
        ///   values, in addition to using the object returned by <see cref="CommandLineParser.Parse(string[])"/>.
        /// </para>
        /// <para>
        ///   If an argument was supplied on the command line, the <see cref="Value"/> property will equal the
        ///   supplied value after conversion to the type specified by the <see cref="ArgumentType"/> property,
        ///   and the <see cref="HasValue"/> property will be <see langword="true"/>.
        /// </para>
        /// <para>
        ///   If an optional argument was not supplied, the <see cref="Value"/> property will equal
        ///   the <see cref="DefaultValue"/> property, and <see cref="HasValue"/> will be <see langword="false"/>.
        /// </para>
        /// </remarks>
        public object Value
        {
            get
            {
                // If this property is accessed from the CommandLineParser.ArgumentParsed event handler and this is
                // an array property, we need to convert our temporary list of values to an array. SetValue will set
                // _value back to null again if another value is added so we never return a stale array.
                if( HasValue && _value == null && _arrayValues != null )
                    ConvertValueIfArray(false);
                return _value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value of this argument was supplied on the command line in the last
        /// call to <see cref="CommandLineParser.Parse(string[],int)"/>.
        /// </summary>
        /// <value>
        /// 	<see langword="true"/> if this argument's value was supplied on the command line when the arguments were parsed; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        ///   Use this property to determine whether or not an argument was supplied on the command line, or was
        ///   assigned its default value.
        /// </para>
        /// <para>
        ///   When an optional argument is not supplied on the command line, the <see cref="Value"/> property will be equal
        ///   to the <see cref="DefaultValue"/> property, and <see cref="HasValue"/> will be <see langword="false"/>.
        /// </para>
        /// <para>
        ///   It is however possible for the user to supply a value on the command line that matches the default value.
        ///   In that case, although the <see cref="Value"/> property will still be equal to the <see cref="DefaultValue"/>
        ///   property, the <see cref="HasValue"/> property will be <see langword="true"/>. This allows you to distinguish
        ///   between an argument that was supplied or omitted even if the supplied value matches the default.
        /// </para>
        /// </remarks>
        public bool HasValue { get; internal set; }

        /// <summary>
        /// Converts the specified string to the argument type, as specified in the <see cref="ArgumentType"/> property.
        /// </summary>
        /// <param name="culture">The culture to use to convert the argument.</param>
        /// <param name="argument">The string to convert.</param>
        /// <returns>The argument, converted to the type specified by the <see cref="ArgumentType"/> property.</returns>
        /// <remarks>
        /// <para>
        ///   The <see cref="TypeConverter"/> for the type specified by <see cref="ArgumentType"/> is used to do the conversion.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="culture"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="CommandLineArgumentException">
        ///   <paramref name="argument"/> could not be converted to the type specified in the <see cref="ArgumentType"/> property.
        /// </exception>
        public object ConvertToArgumentType(CultureInfo culture, string argument)
        {
            if( culture == null )
                throw new ArgumentNullException("culture");

            try
            {
                return _converter.ConvertFrom(null, culture, argument);
            }
            catch( NotSupportedException ex )
            {
                throw new CommandLineArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.ArgumentConversionErrorFormat, argument, ArgumentName, ValueDescription), ArgumentName, CommandLineArgumentErrorCategory.ArgumentValueConversion, ex);
            }
            catch( FormatException ex )
            {
                throw new CommandLineArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.ArgumentConversionErrorFormat, argument, ArgumentName, ValueDescription), ArgumentName, CommandLineArgumentErrorCategory.ArgumentValueConversion, ex);
            }
            catch( Exception ex )
            {
                // Yeah, I don't like catching Exception, but unfortunately BaseNumberConverter (e.g. used for int) can *throw* a System.Exception (not a derived class) so there's nothing I can do about it.
                if( ex.InnerException is FormatException )
                    throw new CommandLineArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.ArgumentConversionErrorFormat, argument, ArgumentName, ValueDescription), ArgumentName, CommandLineArgumentErrorCategory.ArgumentValueConversion, ex);
                else
                    throw;
            }
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="CommandLineArgument"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="CommandLineArgument"/>.</returns>
        /// <remarks>
        /// <para>
        ///   The string value matches the way the argument is displayed in the usage help's command line syntax
        ///   when using the default <see cref="WriteUsageOptions"/>.
        /// </para>
        /// </remarks>
        public override string ToString()
        {
            return ToString(new WriteUsageOptions());
        }

        internal string ToString(WriteUsageOptions options)
        {
            string argumentName = _parser.ArgumentNamePrefixes[0] + ArgumentName;
            if( Position != null )
                argumentName = string.Format(CultureInfo.CurrentCulture, options.OptionalArgumentFormat, argumentName); // for positional parameters, the name itself is optional

            string argument = argumentName;
            if( !IsSwitch )
            {
                char separator = (_parser.AllowWhiteSpaceValueSeparator && options.UseWhiteSpaceValueSeparator) ? ' ' : CommandLineParser.NameValueSeparator;
                string argumentValue = string.Format(CultureInfo.CurrentCulture, options.ValueDescriptionFormat, ValueDescription);
                argument = argumentName + separator + argumentValue;
            }
            if( ArgumentType.IsArray )
                argument += options.ArraySuffix;
            if( IsRequired )
                return argument;
            else
                return string.Format(CultureInfo.CurrentCulture, options.OptionalArgumentFormat, argument);
        }

        internal void SetValue(CultureInfo culture, string value)
        {
            object convertedValue;
            if( value == null )
            {
                if( IsSwitch )
                    convertedValue = true;
                else
                    throw new CommandLineArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.MissingValueForNamedArgumentFormat, ArgumentName), ArgumentName, CommandLineArgumentErrorCategory.MissingNamedArgumentValue);
            }
            else
                convertedValue = ConvertToArgumentType(culture, value);

            if( ArgumentType.IsArray )
            {
                if( !HasValue )
                {
                    Debug.Assert(_arrayValues == null);
                    _arrayValues = new List<object>();
                    HasValue = true;
                }
                _arrayValues.Add(convertedValue);
                _value = null; // Set to null so that if the CommandLineParser.ArgumentParsed event handler accesses Value, it gets an up-to-date array.
            }
            else if( !HasValue || _parser.AllowDuplicateArguments )
            {
                _value = convertedValue;
                HasValue = true;
            }
            else
                throw new CommandLineArgumentException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.DuplicateArgumentFormat, ArgumentName), ArgumentName, CommandLineArgumentErrorCategory.DuplicateArgument);
        }

        internal void ConvertValueIfArray(bool clearValues)
        {
            // This is called after parsing is complete to convert the temporary list of values into an array of the correct type.
            if( ArgumentType.IsArray && HasValue )
            {
                Array values = Array.CreateInstance(ArgumentType.GetElementType(), _arrayValues.Count);
                for( int x = 0; x < _arrayValues.Count; ++x )
                    values.SetValue(_arrayValues[x], x);
                _value = values;
                if( clearValues )
                    _arrayValues = null;
            }
        }

        internal static CommandLineArgument Create(CommandLineParser parser, ParameterInfo parameter)
        {
            if( parser == null )
                throw new ArgumentNullException("parser");
            if( parameter == null )
                throw new ArgumentNullException("parameter");
            ArgumentNameAttribute argumentNameAttribute = (ArgumentNameAttribute)Attribute.GetCustomAttribute(parameter, typeof(ArgumentNameAttribute));
            string argumentName = argumentNameAttribute == null ? parameter.Name : argumentNameAttribute.ArgumentName;
            DescriptionAttribute descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(parameter, typeof(DescriptionAttribute));
            string description = descriptionAttribute == null ? null : descriptionAttribute.Description;
            object defaultValue = ((parameter.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault) ? parameter.DefaultValue : null;
            ValueDescriptionAttribute valueDescriptionAttribute = (ValueDescriptionAttribute)Attribute.GetCustomAttribute(parameter, typeof(ValueDescriptionAttribute));
            string valueDescription = valueDescriptionAttribute == null ? null : valueDescriptionAttribute.ValueDescription;

            return new CommandLineArgument(parser, null, argumentName, parameter.ParameterType, parameter.Position, !parameter.IsOptional, defaultValue, description, valueDescription);
        }

        internal static CommandLineArgument Create(CommandLineParser parser, PropertyInfo property)
        {
            if( parser == null )
                throw new ArgumentNullException("parser");
            if( property == null )
                throw new ArgumentNullException("property");
            CommandLineArgumentAttribute attribute = (CommandLineArgumentAttribute)Attribute.GetCustomAttribute(property, typeof(CommandLineArgumentAttribute));
            if( attribute == null )
                throw new ArgumentException(Properties.Resources.MissingArgumentAttribute, "property");
            string argumentName = attribute.ArgumentName ?? property.Name;
            object defaultValue = attribute.DefaultValue;
            DescriptionAttribute descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(property, typeof(DescriptionAttribute));
            string description = descriptionAttribute == null ? null : descriptionAttribute.Description;
            string valueDescription = attribute.ValueDescription; // If null, the ctor will sort it out.

            return new CommandLineArgument(parser, property, argumentName, property.PropertyType, attribute.Position < 0 ? null : (int?)attribute.Position, attribute.IsRequired, defaultValue, description, valueDescription);
        }

        internal void ApplyPropertyValue(object target)
        {
            if( target == null )
                throw new ArgumentNullException("target");

            // Do nothing for parameter-based values
            if( _property != null )
                _property.SetValue(target, Value, null);
        }

        internal void Reset()
        {
            _value = DefaultValue;
            HasValue = false;
            _arrayValues = null;
        }

        private static string GetFriendlyTypeName(Type type)
        {
            // This is used to generate a value description from a type name if no custom value description was supplied.
            if( type.IsGenericType )
            {
                // We print Nullable<T> as just T.
                if( type.GetGenericTypeDefinition() == typeof(Nullable<>) )
                    return GetFriendlyTypeName(type.GetGenericArguments()[0]);
                else
                {
                    StringBuilder name = new StringBuilder(type.FullName.Length);
                    name.Append(type.Name, 0, type.Name.IndexOf("`", StringComparison.Ordinal));
                    name.Append('<');
                    // If only I was targetting .Net 4, I could use string.Join for this.
                    bool first = true;
                    foreach( Type typeArgument in type.GetGenericArguments() )
                    {
                        if( first )
                            first = false;
                        else
                            name.Append(", ");
                        name.Append(GetFriendlyTypeName(typeArgument));
                    }
                    name.Append('>');
                    return name.ToString();
                }
            }
            else
                return type.Name;
        }    
    }
}
