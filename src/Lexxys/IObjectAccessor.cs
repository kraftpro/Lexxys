namespace Lexxys;

/// <summary>
/// Defines the contract for accessing properties of an object, including indexed properties, in a dynamic manner.
/// </summary>
public interface IObjectAccessor
{
	/// <summary>
	/// Gets the collection of property names.
	/// </summary>
	IReadOnlyCollection<string> Properties { get; }

	/// <summary>
	/// Gets the collection of property names that are indexed.
	/// </summary>
	IReadOnlyCollection<string> IndexedProperties { get; }

	/// <summary>
	/// Attempts to get the value associated with the specified name.
	/// </summary>
	/// <param name="name">The name of the value to retrieve.</param>
	/// <param name="result">When this method returns, contains the value associated with the specified name, if found; otherwise, <see
	/// langword="null"/>.</param>
	/// <returns><see langword="true"/> if the property was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetValue(string name, out object? result);

	/// <summary>
	/// Attempts to retrieve a value using the specified name and index.
	/// </summary>
	/// <param name="name">The name used to identify the value.</param>
	/// <param name="index">The index used to locate the value.</param>
	/// <param name="result">When this method returns, contains the retrieved value if found; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the indexed property was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetValue(string name, object index, out object? result);

	/// <summary>
	/// Attempts to set a value with the specified name.
	/// </summary>
	/// <param name="name">The name of the value to set.</param>
	/// <param name="value">The value to set.</param>
	/// <returns><see langword="true"/> if the property was found; otherwise, <see langword="false"/>.</returns>
	bool TrySetValue(string name, object? value);

	/// <summary>
	/// Attempts to set a value at the specified name and index.
	/// </summary>
	/// <param name="name">The name identifier.</param>
	/// <param name="index">The index location.</param>
	/// <param name="value">The value to set.</param>
	/// <returns><see langword="true"/> if the indexed property was found; otherwise, <see langword="false"/>.</returns>
	bool TrySetValue(string name, object index, object? value);
}

/// <summary>
/// Provides dynamic access to object properties and indexed properties.
/// </summary>
public interface IObjectTypeAccessor
{
	/// <summary>
	/// Gets the collection of property names.
	/// </summary>
	IReadOnlyCollection<string> Properties { get; }

	/// <summary>
	/// Gets the collection of property names that are indexed.
	/// </summary>
	IReadOnlyCollection<string> IndexedProperties { get; }

	/// <summary>
	/// Attempts to get a value from the specified instance by name.
	/// </summary>
	/// <param name="instance">The object instance from which to retrieve the value.</param>
	/// <param name="name">The name of the value to retrieve.</param>
	/// <param name="result">When this method returns, contains the value if found; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the property was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetValue(object? instance, string name, out object? result);

	/// <summary>
	/// Attempts to get the value of an indexed property from the specified object instance.
	/// </summary>
	/// <param name="instance">The object instance containing the indexed property.</param>
	/// <param name="name">The name of the indexed property to retrieve.</param>
	/// <param name="index">The index of the property value to retrieve.</param>
	/// <param name="result">When this method returns, contains the property value if the property was found; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the indexed property was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetValue(object? instance, string name, object index, out object? result);

	/// <summary>
	/// Attempts to set a property or field value on the specified instance.
	/// </summary>
	/// <param name="instance">The object instance on which to set the value.</param>
	/// <param name="name">The name of the property or field to set.</param>
	/// <param name="value">The value to assign.</param>
	/// <returns><see langword="true"/> if the property was found; otherwise, <see langword="false"/>.</returns>
	bool TrySetValue(object? instance, string name, object? value);

	/// <summary>
	/// Attempts to set the value of an indexed property or element.
	/// </summary>
	/// <param name="instance">The object instance containing the indexed property or element.</param>
	/// <param name="name">The name of the property or member.</param>
	/// <param name="index">The index for the property or element.</param>
	/// <param name="value">The value to set.</param>
	/// <returns><see langword="true"/> if the indexed property was found; otherwise, <see langword="false"/>.</returns>
	bool TrySetValue(object? instance, string name, object index, object? value);
}
