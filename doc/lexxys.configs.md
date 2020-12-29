# Lexxys configuration settings

## Database Connection
### database.connection

```
database
  connection [reference]
    :parameter value
    ...
```

##### Example:
```
database
  connection server=DB1, database=Students
```
```
database
  connection
    :server    DB1
    :database  Students
    :user      sa
    :pwd       1st-strong-password
    :async     true
    :connectionTimeout 10s
    :commandTimeout    12m
    :app       StudentsApp
```

## Database References Validations
### lexxys.validation.reference
- cacheCapacity - total number items in the cache (default `8*1024`)
- cacheTimeout - time-out value for cache items (default `10s`)
- validate - indicates that the reference validation is required (default `true`)

## Extra mime types
### lexxys.mime
- map [collection]
  - ext - the file extension
  - type - the mime type associated with the file extension

##### Example:
```
lexxys
  mime
    %map ext  type
    -   .cpp  text/plain
    -   .ac3  audio/ac3
```

## Cryptographic providers
### lexxys.crypto.providers
- item [collection]
  - type - hasher|encryptor|decryptor
  - name - name of the provider
  - class - name of the class which implements `IHasherAlgorythm`, `IEncryptorAlgorythm`, or `IDecryptorAlgorythm` interface.
  - assembly - reference to the assembly of the class

##### Example:
```
crypto
  providers
    %item  type   name class                               assembly
    -      hasher MD5  Lexxys.Crypting.Cryptors.MD5.Hasher Lexxys.dll
    ...

```

## Factory Configuration
### lexxys.factory
- import
  - item [collection] list of external assemblies to load at runtime
- ignore
  - item [collection] list of assemblies to ignore (system assemblies)
- synonyms
  - item [collection] list of type sysnonyms
    - key - name of the type sysnonym
    - value - name of the type

## Config configuration
### lexxys.config
- cacheValue indicare that configuration will cache retrieved values for future use (default false)

 ```
 lexxys
   config cacheValue=true
 ```
