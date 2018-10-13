
// A reference implementation of the MSAL interface for authentication and graph interaction
//
// This allows a SPA to get up and running very fast with MSAL, but with little to no actual
// customization in the experience. It just validates that LogIn/LogOut, and graph calls
// are working (which means the live auth stack and registration is correctly hooked up)
//
// To use, include MSAL-support.ts and MSAL-ref.ts, then provide the reference implementation
// to MSAL-support as the interface implemenation.

// this assumes that MSAL-support has already been included, as well as jquery


