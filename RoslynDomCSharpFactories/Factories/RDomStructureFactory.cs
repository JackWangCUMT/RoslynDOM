﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDom.Common;

namespace RoslynDom.CSharp
{
    internal static class RDomStructureFactoryHelper
    {
         // until move to C# 6 - I want to support name of as soon as possible
        [ExcludeFromCodeCoverage]
        private static string nameof<T>(T value) { return ""; }


        public static RDomStructure CreateFrom(SyntaxNode syntaxNode, IDom parent, SemanticModel model, ICreateFromWorker createFromWorker, RDomCorporation corporation)
        {
            var syntax = syntaxNode as StructDeclarationSyntax;
            var newItem = new RDomStructure(syntaxNode, parent, model);
            createFromWorker.StandardInitialize(newItem, syntaxNode, parent, model);
            newItem.Name = newItem.TypedSymbol.Name;

            var newTypeParameters = newItem.TypedSymbol.TypeParametersFrom();
            newItem.TypeParameters.AddOrMoveRange(newTypeParameters);

            var members = ListUtilities.MakeList(syntax, x => x.Members, x => corporation.CreateFrom<ITypeMemberCommentWhite>(x, newItem, model));
            newItem.MembersAll.AddOrMoveRange(members);

            return newItem;
        }

        public static IEnumerable<SyntaxNode> BuildSyntax(RDomStructure item, ICSharpBuildSyntaxWorker buildSyntaxWorker, RDomCorporation corporation)
        {
            // This is identical to Class, but didn't work out reuse here
            var modifiers = item.BuildModfierSyntax();
            var identifier = SyntaxFactory.Identifier(item.Name);
            var node = SyntaxFactory.StructDeclaration(identifier)
                .WithModifiers(modifiers);
            var attributes = buildSyntaxWorker.BuildAttributeSyntax(item.Attributes);
            if (attributes.Any()) { node = node.WithAttributeLists(attributes.WrapInAttributeList()); }
            var itemAsStruct = item as IStructure;
            Guardian.Assert.IsNotNull(itemAsStruct, nameof(itemAsStruct));
            var membersSyntax = itemAsStruct.Members
                        .SelectMany(x => RDomCSharp.Factory.BuildSyntaxGroup(x))
                        .ToList();
            node = node.WithMembers(SyntaxFactory.List(membersSyntax));
            node = node.WithLeadingTrivia(BuildSyntaxHelpers.LeadingTrivia(item));
            // TODO: Class type members and type constraints
            return item.PrepareForBuildSyntaxOutput(node);
        }
    }
    public class RDomStructureTypeMemberFactory
      : RDomTypeMemberFactory<RDomStructure, StructDeclarationSyntax>
    {
        public RDomStructureTypeMemberFactory(RDomCorporation corporation)
            : base(corporation)
        { }

        protected override ITypeMemberCommentWhite CreateItemFrom(SyntaxNode syntaxNode, IDom parent, SemanticModel model)
        {
            return RDomStructureFactoryHelper.CreateFrom(syntaxNode, parent, model, CreateFromWorker, Corporation);
        }
        public override IEnumerable<SyntaxNode> BuildSyntax(IDom item)
        {
            return RDomStructureFactoryHelper.BuildSyntax((RDomStructure)item, BuildSyntaxWorker, Corporation);
        }
    }

    public class RDomStructureStemMemberFactory
           : RDomStemMemberFactory<RDomStructure, StructDeclarationSyntax>
    {
        public RDomStructureStemMemberFactory(RDomCorporation corporation)
            : base(corporation)
        { }

        protected override IStemMemberCommentWhite CreateItemFrom(SyntaxNode syntaxNode, IDom parent, SemanticModel model)
        {
            return RDomStructureFactoryHelper.CreateFrom(syntaxNode, parent, model, CreateFromWorker, Corporation);
        }

        public override IEnumerable<SyntaxNode> BuildSyntax(IDom item)
        {

            return RDomStructureFactoryHelper.BuildSyntax((RDomStructure)item, BuildSyntaxWorker, Corporation);
        }
    }

}
