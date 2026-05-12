// Surface boundary types from Sussudio.Services.Contracts everywhere in the
// Sussudio project so subsystem code can reference IRecordingSink,
// IPreviewFrameSink, IAutomationCommandDispatcher, etc., without per-file
// using directives. Keeps service implementation files dependent on the
// Contracts seam first and any concrete-implementation namespace second.
global using Sussudio.Services.Contracts;
