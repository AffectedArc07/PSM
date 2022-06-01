using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PSM.Core.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.WithMembers)]
public abstract class ModelBase {
  /// <summary>
  /// This method should perform data validation to ensure all data is correct and valid.
  /// </summary>
  /// <returns>Whether validation was successful.</returns>
  public virtual bool ValidateModel() {
    var fields = GetType().GetFields().Where(info => info.IsPublic && !info.IsStatic);
    return fields.All(field => field.GetValue(this) != null);
  }

  public          string Jsonify(bool pretty = false) => JsonConvert.SerializeObject(this, pretty ? Formatting.Indented : Formatting.None);
  public override string ToString()                   => Jsonify();

  /// <summary>
  /// Creates a Data Model from the given JSON for the specified Type.
  /// </summary>
  /// <param name="json">The json value to serialize the model from.</param>
  /// <typeparam name="T">The Type of Data Model to generate and verify.</typeparam>
  /// <exception cref="SerializationException">Serialization for this Data Model Type failed.</exception>
  /// <exception cref="ValidationException">Data Verification failed.</exception>
  public static T FromJson<T>(string json) where T : ModelBase {
    if(JsonConvert.DeserializeObject<T>(json) is not { } val)
      throw new SerializationException();
    if(!val.ValidateModel())
      throw new ValidationException();
    return val;
  }
}
