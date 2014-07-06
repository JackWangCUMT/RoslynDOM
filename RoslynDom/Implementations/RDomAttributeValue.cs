﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDom.Common;

namespace RoslynDom
{
    public class RDomAttributeValue
        : RDomBase<IAttributeValue, AttributeArgumentSyntax, ISymbol>, IAttributeValue
    {
        private LiteralType _literalType;
        private object _value;
        private Type _type;
        private AttributeValueStyle _style;

        internal RDomAttributeValue(
            AttributeArgumentSyntax rawItem,
            AttributeSyntax attributeSyntax,
            RDomBase attributedItem)
            : base(rawItem)
        {
            Initialize();
            var tuple = GetAttributeValueName(rawItem, attributeSyntax);
            Name = tuple.Item1;
            _style = tuple.Item2;
        }

        internal RDomAttributeValue(
             RDomAttributeValue oldRDom)
            : base(oldRDom)
        {
            _literalType = oldRDom._literalType;
            _value = oldRDom._value;
            _type = oldRDom._type;
            _style = oldRDom._style;
        }

        protected override void Initialize()
        {
            base.Initialize();
            var tuple = GetAttributeValueValue(TypedSyntax);
            _value = tuple.Item1;
            _literalType = tuple.Item2;
            _type = _value.GetType();
        }


        protected override bool CheckSameIntent(IAttributeValue other, bool includePublicAnnotations)
        {
            if (!base.CheckSameIntent(other, includePublicAnnotations)) return false;
            var rDomOther = other as RDomAttributeValue;
            if (rDomOther == null) throw new InvalidOperationException();
            if (Name != rDomOther.Name) return false;
            if (_literalType != rDomOther._literalType) return false;
            if (!(_value.Equals(rDomOther._value))) return false;
            return true;
        }

        private Tuple<object, LiteralType> GetAttributeValueValue(
                    AttributeArgumentSyntax arg)
        {
            // TODO: Manage multiple values because of AllowMultiples, param array, or missing symbol 
            var expr = arg.Expression;
            var literalType = LiteralType.Unknown;
            object value = null;
            var literalExpression = expr as LiteralExpressionSyntax;
            if (literalExpression != null)
            { value = GetLiteralValue(literalExpression, ref literalType); }
            else
            {
                var typeExpression = expr as TypeOfExpressionSyntax;
                if (typeExpression != null)
                {
                    literalType = LiteralType.Type;
                    value = GetTypeExpressionValue(typeExpression);
                }
            }
            return new Tuple<object, LiteralType>(value, literalType);
        }

        private object GetTypeExpressionValue(TypeOfExpressionSyntax typeExpression)
        {
            object value = null;
            var typeSyntax = typeExpression.Type;
            var predefinedTypeSyntax = typeSyntax as PredefinedTypeSyntax;
            var identifierNameSyntax = typeSyntax as IdentifierNameSyntax;
            if (predefinedTypeSyntax != null)
            {
                var typeInfo = GetTypeInfo(predefinedTypeSyntax);
                value = new RDomReferencedType(typeInfo, null);
            }
            else if (identifierNameSyntax != null)
            {
                var typeInfo = GetTypeInfo(identifierNameSyntax);
                value = new RDomReferencedType(typeInfo, null);
            }
            else
            {
                // I don't know how to get here, but if I get here, I want to know it :)
                throw new InvalidOperationException();
            }
            return value;
        }

        private object GetLiteralValue(LiteralExpressionSyntax literalExpression, ref LiteralType literalType)
        {
            literalType = RoslynUtilities.LiteralTypeFromSyntaxKind(literalExpression.Token.CSharpKind());
            return literalExpression.Token.Value;
        }

        private static Tuple<string, AttributeValueStyle> GetAttributeValueName(
           AttributeArgumentSyntax arg, AttributeSyntax attributeSyntax)
        {
            string name;
            AttributeValueStyle style;
            if (arg.NameColon != null)
            {
                style = AttributeValueStyle.Colon;
                name = arg.NameColon.Name.ToString().Replace(":", "").Trim();
            }
            else if (arg.NameEquals != null)
            {
                style = AttributeValueStyle.Equals;
                name = arg.NameEquals.Name.ToString();
            }
            else
            {
                style = AttributeValueStyle.Positional;
                name = attributeSyntax.Name.ToString();
            }
            return new Tuple<string, AttributeValueStyle>(name, style);
        }

        public object Value
        {
            get
            {
                return _value;
            }
        }

        public LiteralType ValueType
        {
            get
            {
                return _literalType;
            }
        }

        internal static IEnumerable<IAttributeValue> MakeAttributeValues(
                AttributeSyntax attrib, RDomBase attributedItem)
        {
            var ret = new List<IAttributeValue>();
            if (attrib.ArgumentList != null)
            {
                var arguments = attrib.ArgumentList.Arguments;
                foreach (AttributeArgumentSyntax arg in arguments)
                {
                    ret.Add(new RDomAttributeValue(arg, attrib, attributedItem));
                }
            }
            return ret;
        }

        public override AttributeArgumentSyntax BuildSyntax()
        {
            var argNameSyntax = SyntaxFactory.IdentifierName(Name);
            var kind = RoslynUtilities.SyntaxKindFromLiteralType(_literalType, _value);
            ExpressionSyntax expr = null;
            if (_literalType == LiteralType.Boolean) { expr = SyntaxFactory.LiteralExpression(kind); }
            else
            {
                var methodInfo = ReflectionUtilities.FindMethod(typeof(SyntaxFactory), "Literal", _type);
                if (methodInfo == null) throw new InvalidOperationException();
                var token = (SyntaxToken)methodInfo.Invoke(null, new object[] { _value });
                expr = SyntaxFactory.LiteralExpression(kind, token);
            }

            if (_style == AttributeValueStyle.Colon )
            {
                return SyntaxFactory.AttributeArgument(
                    null,
                    SyntaxFactory.NameColon(argNameSyntax), 
                    expr);
            }
            else if (_style == AttributeValueStyle.Positional )
            {
                return SyntaxFactory.AttributeArgument( expr);
            }
            else 
            {
                return SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(argNameSyntax),
                    null, expr);
            }
        }
    }
}