# azure-functions-monitoring
Testing Queue Trigger with App Insights

The idea of this repo is to have a Producer console app, and an Azure Function that receive those messages.

The producer is an standard console to demonstrate what configuration needs to be done when the framework does not help much, (opposite to MVC)

In the other hand, Azure Function is expected to do everything for you.

# Application Map
Application Map constructs an `application node` for each unique `cloud role name` present in your request telemetry and a `dependency node` for each unique combination of `type`, `target`, and `cloud role name` in your dependency telemetry. 

Application Insights SDK automatically adds the cloud role name property to the telemetry emitted by components.

# Internals
`Components` in Application Insights are expected to be identified by

```
telemetryClient.Context.Cloud.RoleName = "ProducerApplication";

telemetryClient.Context.Cloud.RoleInstance = "localhost";
```
Those settings will be reflected in the application map

The links between components are `dependencies`. Thosecan be done as `Dependency` or `Operations`:

```
telemetryClient.StartOperation<T>  where T : OperationTelemetry

telemetryClient.TrackDependency(...)
```

OperationTelemetry has two inheritance childs, DependencyTelemetry or RequestTelemetry
DependencyTelemetry: Notifies App Insights that it is an outbound call
RequestTelemetry:  Notifies App Insights that it is an inbound call

so under a communication between two components, the caller should start an operation of type DependencyTelemetry, and the callee RequestTelemetry

To correlate operations App Insights uses two fields:

OperationId
OperationParentID

# References
https://github.com/Azure/azure-functions-host/issues/5530

# Useful Kusto queries:
```
// get all logs from all components. Start by the cloud_RoleName and then include all operations_ids
let operationIds= dynamic(["756f9174a6957c46a5b2a8fbc96a5774", "4d89c1487dc6cf4a", "e800749928382a4e", "de27d20879b7b348", "1ca942e69f136941"]);
union *
| where timestamp > ago(60m)
| where operation_Id in (operationIds)
    or operation_ParentId in (operationIds)
//| where cloud_RoleName == 'Producer for functionwithoutoperationsqueue'
| project timestamp, itemType, operation_Name, operation_Id, operation_ParentId, cloud_RoleName, message=strcat(message, "; ", name, "; ", target)

```