﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.TestCommon;
using Microsoft.OData.Edm;
using Microsoft.TestCommon;

namespace System.Web.OData.Builder
{
    public class EntitySetLinkConfigurationTest
    {
        [Fact]
        public void CanConfigureAllLinksViaIdLink()
        {
            // Arrange
            ODataModelBuilder builder = GetCommonModel();
            var expectedEditLink = "http://server/service/Products(15)";

            var products = builder.EntitySet<EntitySetLinkConfigurationTest_Product>("Products");
            products.HasIdLink(c =>
                new Uri(string.Format(
                    "http://server/service/Products({0})",
                    c.GetPropertyValue("ID"))
                ),
                followsConventions: false);

            var actor = builder.EntitySets.Single();
            var model = builder.GetEdmModel();
            var productType = model.SchemaElements.OfType<IEdmEntityType>().Single();
            var productsSet = model.SchemaElements.OfType<IEdmEntityContainer>().Single().EntitySets().Single();
            var productInstance = new EntitySetLinkConfigurationTest_Product { ID = 15 };
            var serializerContext = new ODataSerializerContext { Model = model, NavigationSource = productsSet };
            var entityContext = new EntityInstanceContext(serializerContext, productType.AsReference(), productInstance);
            var linkBuilderAnnotation = new NavigationSourceLinkBuilderAnnotation(actor);

            // Act
            var selfLinks = linkBuilderAnnotation.BuildEntitySelfLinks(entityContext, ODataMetadataLevel.Default);

            // Assert
            Assert.NotNull(selfLinks.EditLink);
            Assert.Equal(expectedEditLink, selfLinks.EditLink.ToString());
            Assert.NotNull(selfLinks.ReadLink);
            Assert.Equal(expectedEditLink, selfLinks.ReadLink.ToString());
            Assert.NotNull(selfLinks.IdLink);
            Assert.Equal(expectedEditLink, selfLinks.IdLink.ToString());
        }

        [Fact]
        public void CanConfigureLinksIndependently()
        {
            // Arrange
            ODataModelBuilder builder = GetCommonModel();
            var expectedEditLink = "http://server1/service/Products(15)";
            var expectedReadLink = "http://server2/service/Products/15";
            var expectedIdLink = "http://server3/service/Products(15)";

            var products = builder.EntitySet<EntitySetLinkConfigurationTest_Product>("Products");
            products.HasEditLink(c => new Uri(
                string.Format(
                    "http://server1/service/Products({0})",
                    c.GetPropertyValue("ID")
                )
            ),
            followsConventions: false);
            products.HasReadLink(c => new Uri(
                string.Format(
                    "http://server2/service/Products/15",
                    c.GetPropertyValue("ID")
                )
            ),
            followsConventions: false);
            products.HasIdLink(c =>
                new Uri(string.Format(
                    "http://server3/service/Products({0})",
                    c.GetPropertyValue("ID"))
                ),
            followsConventions: false
            );

            var actor = builder.EntitySets.Single();
            var model = builder.GetEdmModel();
            var productType = model.SchemaElements.OfType<IEdmEntityType>().Single();
            var productsSet = model.SchemaElements.OfType<IEdmEntityContainer>().Single().EntitySets().Single();
            var productInstance = new EntitySetLinkConfigurationTest_Product { ID = 15 };
            var serializerContext = new ODataSerializerContext { Model = model, NavigationSource = productsSet };
            var entityContext = new EntityInstanceContext(serializerContext, productType.AsReference(), productInstance);

            // Act
            var editLink = actor.GetEditLink().Factory(entityContext);
            var readLink = actor.GetReadLink().Factory(entityContext);
            var idLink = actor.GetIdLink().Factory(entityContext);

            // Assert
            Assert.NotNull(editLink);
            Assert.Equal(expectedEditLink, editLink.ToString());
            Assert.NotNull(readLink);
            Assert.Equal(expectedReadLink, readLink.ToString());
            Assert.NotNull(idLink);
            Assert.Equal(expectedIdLink, idLink.ToString());
        }

        [Fact]
        public void FailingToConfigureLinksResultsInNullLinks()
        {
            // Arrange
            ODataModelBuilder builder = GetCommonModel();
            var actor = builder.EntitySets.Single();
            var model = builder.GetEdmModel();

            // Act & Assert
            Assert.Null(actor.GetEditLink());
            Assert.Null(actor.GetReadLink());
            Assert.Null(actor.GetIdLink());
        }

        [Fact]
        public void FailingToConfigureNavigationLinks_Results_In_ArgumentException_When_BuildingNavigationLink()
        {
            // Arrange
            ODataModelBuilder builder = ODataModelBuilderMocks.GetModelBuilderMock<ODataModelBuilder>();
            builder.EntitySet<EntitySetLinkConfigurationTest_Product>("Products").HasManyBinding(p => p.Orders, "Orders");
            var model = builder.GetEdmModel();

            IEdmEntitySet products = model.EntityContainer.EntitySets().Single(s => s.Name == "Products");
            IEdmNavigationProperty ordersProperty = products.EntityType().DeclaredNavigationProperties().Single();
            var linkBuilder = model.GetNavigationSourceLinkBuilder(products);

            // Act & Assert
            Assert.ThrowsArgument(
                () => linkBuilder.BuildNavigationLink(new EntityInstanceContext(), ordersProperty, ODataMetadataLevel.Default),
                "navigationProperty",
                "No NavigationLink factory was found for the navigation property 'Orders' from entity type 'System.Web.OData.Builder.EntitySetLinkConfigurationTest_Product' on entity set or singleton 'Products'. " +
                "Try calling HasNavigationPropertyLink on the NavigationSourceConfiguration.");
        }

        private ODataModelBuilder GetCommonModel()
        {
            ODataModelBuilder builder = new ODataModelBuilder();
            var products = builder.EntitySet<EntitySetLinkConfigurationTest_Product>("Products");
            var product = products.EntityType;
            product.HasKey(p => p.ID);
            product.Property(p => p.Name);
            product.Property(p => p.Price);
            product.Property(p => p.Cost);

            return builder;
        }

        class EntitySetLinkConfigurationTest_Product
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public Decimal Price { get; set; }
            public Decimal Cost { get; set; }

            public EntitySetLinkConfigurationTest_Order[] Orders { get; set; }
        }

        class EntitySetLinkConfigurationTest_Order
        {
            public string ID { get; set; }
        }
    }
}
