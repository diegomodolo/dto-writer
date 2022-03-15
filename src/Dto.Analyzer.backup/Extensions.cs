﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dto.Analyzer
{
  public static class Extensions
  {
    public static string ToLowerCamelCase(this string str)
    {
      return $"{str[0].ToString().ToLower()}{str.Substring(1)}";
    }
    
    public static ClassDeclarationSyntax AddDataContractAttribute(this ClassDeclarationSyntax classDeclaration)
    {
      return classDeclaration.AddAttributeLists(new[]
      {
        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
          SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(Constants.Attribute.DataContract))))
      });
    }

    public static PropertyDeclarationSyntax AddDataMemberAttribute(this PropertyDeclarationSyntax prop)
    {
      return prop.AddAttributeLists(new[]
      {
        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
          SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(Constants.Attribute.DataMember))))
      });
    }

    public static PropertyDeclarationSyntax AddJsonPropertyAttribute(this PropertyDeclarationSyntax prop)
    {
      var propName = prop.Identifier.ValueText;
      return prop.AddAttributeLists(new[]
      {
        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(Constants.Attribute.JsonProperty))
              .WithArgumentList(
                SyntaxFactory.AttributeArgumentList(
                  SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(
                      SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(propName.ToLowerCamelCase())
                      )
                    )
                  )
                )
              )
          )
        )
      });
    }

    public static ClassDeclarationSyntax RemoveAttribute(this ClassDeclarationSyntax classDeclaration, string attributeName)
    {
      return (ClassDeclarationSyntax)new AttributeRemover(attributeName).Visit(classDeclaration);
    }

    public static PropertyDeclarationSyntax RemoveAttribute(this PropertyDeclarationSyntax prop, string attributeName)
    {
      return (PropertyDeclarationSyntax)new AttributeRemover(attributeName).Visit(prop);
    }

    public static bool HasAttribute(this PropertyDeclarationSyntax prop, string attributeName)
    {
      return prop.DescendantNodes()
        .OfType<AttributeListSyntax>()
        .Any(l => l.Attributes.Any(a =>
          a.Name.NormalizeWhitespace().ToFullString()
            .Equals(attributeName)));
    }

    public static bool HasAttribute(this ClassDeclarationSyntax classDeclaration, string attributeName)
    {
      return classDeclaration.DescendantNodes()
        .OfType<AttributeListSyntax>()
        .Any(l => l.Attributes.Any(a =>
          a.Name.NormalizeWhitespace().ToFullString()
            .Equals(attributeName)));
    }

    public static bool IsDto(this ClassDeclarationSyntax classDeclaration)
    {
      var className = classDeclaration.Identifier.ValueText;
      return !string.IsNullOrWhiteSpace(className) && className.EndsWith(Constants.DtoSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private class AttributeRemover : CSharpSyntaxRewriter
    {
      private readonly string _attributeName;
      public AttributeRemover(string attributeName)
      {
        _attributeName = attributeName;
      }

      public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
      {
        var propAttribute =
          node.Attributes.FirstOrDefault(a => a.Name.NormalizeWhitespace().ToFullString().Equals(_attributeName));
        if (propAttribute != null)
        {
          if (node.Parent is PropertyDeclarationSyntax || node.Parent is ClassDeclarationSyntax)
          {
            if (node.Attributes.Count == 1)
              return null;

            return node.RemoveNode(propAttribute, SyntaxRemoveOptions.KeepNoTrivia);
          }
        }

        return base.VisitAttributeList(node);
      }
    }
  }
}
