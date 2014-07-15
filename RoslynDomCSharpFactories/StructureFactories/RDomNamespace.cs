﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDom.Common;

namespace RoslynDom
{

    public class RDomNamespaceStemMemberFactory
           : RDomStemMemberFactory<INamespace, NamespaceDeclarationSyntax>
    {
        public override void InitializeItem(INamespace newItem, NamespaceDeclarationSyntax syntax)
        {
            // Qualified name unbundles namespaces, and if it's defined together, we want it together here. 
            // Thus, this replaces hte base Initialize name with the correct one
            newItem.Name = syntax.NameFrom();
            if (newItem.Name.StartsWith("@")) { newItem.Name = newItem.Name.Substring(1); }
            var members = ListUtilities.MakeList(syntax, x => x.Members, x => RDomFactoryHelper.StemMemberFactoryHelper.MakeItem(x));
            var usings = ListUtilities.MakeList(syntax, x => x.Usings, x => RDomFactoryHelper.StemMemberFactoryHelper.MakeItem(x));
            foreach (var member in members)
            { newItem.AddOrMoveStemMember(member); }
            foreach (var member in usings)
            { newItem.AddOrMoveStemMember(member); }
        }
        public override IEnumerable<SyntaxNode> BuildSyntax(IStemMember item)
        {
            var identifier = SyntaxFactory.IdentifierName(item.Name);
            var node = SyntaxFactory.NamespaceDeclaration (identifier);
            var itemAsNamespace = item as INamespace;
            if (itemAsNamespace == null) { throw new InvalidOperationException(); }
            var usingsSyntax = itemAsNamespace.Usings
                        .Select(x => RDomFactory.BuildSyntaxGroup(x))
                        .OfType<UsingDirectiveSyntax>()
                        .ToList();
            if (usingsSyntax.Count() > 0) { node = node.WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>(usingsSyntax)); }
            var membersSyntax = itemAsNamespace.StemMembers
                        .Select(x => RDomFactory.BuildSyntaxGroup(x))
                        .OfType<MemberDeclarationSyntax>()
                        .ToList();
            if (usingsSyntax.Count() > 0) { node = node.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(membersSyntax)); }
            return new SyntaxNode[] { node.NormalizeWhitespace() };
        }
    }

}