export interface KernelClient {    
    GetVariable(variableName: string): any ;    
}

export function createClient(): KernelClient{
    return {
        GetVariable(variableName: string): any{
            return 1;
        }
    }
}