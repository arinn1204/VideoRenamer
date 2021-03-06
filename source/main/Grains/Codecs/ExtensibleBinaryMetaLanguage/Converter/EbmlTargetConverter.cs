﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Attributes;
using Grains.Codecs.ExtensibleBinaryMetaLanguage.Exceptions;

namespace Grains.Codecs.ExtensibleBinaryMetaLanguage.Converter
{
	public static class EbmlTargetConverter
	{
		public static TTarget CreateTarget<TTarget>(
			IEnumerable<(string name, object? value)> values)
			where TTarget : new()
		{
			var target = new TTarget();
			foreach (var groupedValues in values.GroupBy(g => g.name))
			{
				var name = groupedValues.Key;
				var value = groupedValues.Select(s => s.value).ToArray();
				var propertyByName = typeof(TTarget).GetProperty(name);
				var propertyByAttribute = GetPropertyByAttribute<TTarget>(name);

				var propertyToSet = DeterminePropertyToSet(
					propertyByAttribute,
					propertyByName,
					name,
					typeof(TTarget).Name);

				var valueToSet = propertyToSet.PropertyType != typeof(string) &&
				                 propertyToSet.PropertyType
				                              .GetInterfaces()
				                              .Any(
					                               a =>
					                               {
						                               var isIEnumerable = typeof(IEnumerable);
						                               return a == isIEnumerable;
					                               })
					? EbmlEnumerableConverter.HandleMultipleObjects(propertyToSet, value)
					: EbmlObjectConverter.HandleSingleObject(propertyToSet, value[0]);


				propertyToSet?.SetValue(target, valueToSet);
			}

			return target;
		}

		private static PropertyInfo DeterminePropertyToSet(
			PropertyInfo propertyByAttribute,
			PropertyInfo? propertyByName,
			string name,
			string containingObjectName)
		{
			var propertyToSet = (propertyByAttribute == null, propertyByName == null,
			                     propertyByAttribute?.Name == propertyByName?.Name) switch
			                    {
				                    (true, false, _)     => propertyByName,
				                    (false, true, _)     => propertyByAttribute,
				                    (false, false, true) => propertyByName,
				                    (false, false, false) => throw new EbmlConverterException(
					                    $"Ambiguous match. Element name of '{name}' associated with '{propertyByAttribute?.Name}' and property name '{name}' in '{containingObjectName}'."),
				                    (true, true, _) => throw new EbmlConverterException(
					                    $"There is no element with the name '{name}' in '{containingObjectName}'.")
			                    };
			return propertyToSet!;
		}

		private static PropertyInfo GetPropertyByAttribute<TTarget>(string name)
			where TTarget : new()
		{
			var propertyByAttribute =
				typeof(TTarget).GetProperties()
				               .Where(
					                w => w.CustomAttributes.Any(
						                a => a.AttributeType ==
						                     typeof(EbmlElementAttribute) &&
						                     (string) a
						                             .ConstructorArguments
						                             .FirstOrDefault()
						                             .Value! ==
						                     name))
				               .ToList();

			var propertyToReturn = propertyByAttribute.Count <= 1
				? propertyByAttribute.FirstOrDefault()
				: throw new EbmlConverterException(
					$"Ambiguous match. There are multiple elements with name '{name}'.");

			return propertyToReturn;
		}
	}
}