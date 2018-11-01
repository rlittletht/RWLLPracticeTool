# RWLLPracticeTool
Redmond West Little League practice scheduling website

There are 4 axes for configuration:
	Build flavor (debug | release)
	Hosting location (localhost | stagehost | prodhost)
	Service to bind to (localsvc| stagesvc | prodsvc)
	Data to bind to (localdata | stagesvc | prodsvc)
	
(for stagesvc, we might someday have multiple staging, so stage1svc, stage2svc, etc)

We will create configurations as we need them, but nominally we have the combinatoric explosion that you would expect...

DebugLocalSvcLocalData
DebugLocalSvcStageData
DebugLocalSvcProdData
DebugStageSvcStageData
DebugStageSvcProdData
ReleaseProdSvcProdData

(note that no "later" stage can refer to an "earlier" stage -- Release can only use Prod service and data.  Stage can only reference Stage or Prod. Local can reference everything.


