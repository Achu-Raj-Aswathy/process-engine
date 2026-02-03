C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\ProcessEngine

# 1 Create following projects
Under
C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\ProcessEngine

BizFirst.Ai.ProcessEngine.Api
BizFirst.Ai.ProcessEngine.Api.Base
BizFirst.Ai.ProcessEngine.Domain
BizFirst.Ai.ProcessEngine.Service

C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\AiSession
BizFirst.Ai.AiSession.Domain
BizFirst.Ai.AiSession.Service
Move these classes C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\ProcessEngine\BizFirst.Ai.ProcessEngine.Domain\src\Session\Hierarchy into BizFirst.Ai.AiSession.Domain

And all related classes there.
Also change the Session object names to AiSession. Example AccountSession to AccountAiSession


# 2 Reference Projects:
 Learn about the references from here C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\ProcessStudio

 The data structure is here: C:\BizFirstGO_FI_AI\BizFirstFiDB\BizFirstFiV3DB\BizFirstFiV3DB\dbo\Tables

 # 3 Request Data Models

When any user request comes from the UI or WebHooks etc will create a RequestSession. A RequestSession has a UserSession. The UserSession has AppSession. An AppSession has an AccountSession. An AccountSession has PlatformSession

The idea is to cache these sessions or make these sessions available everywhere in the execution context through Accessor


# 4 Context Data Models
The process engine has the following api end points
ExecuteProcess, 
ExecuteProcessThread, 
ExecuteProcessElement
I also need end points to Pause or cancel these execution requests
I need end points to get stat and trace and execution progress etc from the Process_ProcessExecutions , Process_ProcessElementExecutions and Process_ProcessThreadExecutions

# 5. Process_ProcessElements (Nodes), Process_Processes,Process_ProcessThreads, Process_Connections (Edges) carry all workflow related information.

The workflow processing needs to happen through process orchastration service. May be use a OrchastrationProcessor class

# 6. Reference: n8n is an open source project and they have a very similar project. We might want to take inspiration from n8n

# 7. Every ProcessElement needs to have an IProcessElementExecution interface and and for every category of Node. 

Type of nodes can be learned from here
C:\BizFirstGO_FI_AI\BizFirstAiStudio\src\workflow-editor-module

# 8. I am building an enterprise grade process orchastration. 
Some of the Nodes are low trusted and cannot be ran from the same server and we will have to transfer the execution to other servers. When that happens, the context and memory will also have to be available in the other sessions.

# 9. The job of the service project is only the orchastration. Application data and Agent execution etc is the responsibility other projects.

# 10. AIExt_Connectors has the actual configuration information required to actually execute the node
C:\BizFirstGO_FI_AI\BizFirstFiDB\BizFirstFiV3DB\BizFirstFiV3DB\dbo\Tables\AIExt_Connectors.sql


# 11. Context objects
Every Execution element must have a context class ProcessElementContext ProcessThreadContext etc. 

# 12. detailed design approach
 Learn all these things and create a detailed design approach here. Do it fast. Also do deep research. I want the best design. 

 # 13
 write to this C:\BizFirstGO_FI_AI\BizFirstPayrollV3\src\mvc-server\Ai\ProcessEngine\reqs\req01_analysis.md




