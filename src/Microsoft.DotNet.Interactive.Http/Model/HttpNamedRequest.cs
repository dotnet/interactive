#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reactive.Concurrency;
using System.Xml.XPath;

namespace Microsoft.DotNet.Interactive.Http;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpNamedRequest
{
    internal HttpNamedRequest(HttpRequestNode httpRequestNode, HttpResponse response)
    {
        RequestNode = httpRequestNode;
        Name = RequestNode.TryGetCommentNamedRequestNode()?.ValueNode?.Text;
        Response = response;
    }

    public string? Name { get; private set; }

    private HttpRequestNode RequestNode;

    private HttpResponse Response;

    public HttpBindingResult<object?> ResolvePath(string[] path, HttpExpressionNode node)
    {
        if (path.Length < 4)
        {
            return node.CreateBindingFailure(HttpDiagnostics.InvalidNamedRequestPath(node.Text));
        }

        if (path[0] != Name)
        {
            return node.CreateBindingFailure(HttpDiagnostics.InvalidNamedRequestPath(node.Text));
        }


        if (path[1] == "request")
        {
            if (path[2] == "body")
            {
                if (path[3] == "$")
                {
                    if (RequestNode.BodyNode is null)
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest());
                    }

                    var requestJSON = JsonNode.Parse(RequestNode.BodyNode.Text);

                    if (requestJSON is not null)
                    {
                        var resolvedPath = ResolveJsonPath(requestJSON, path, 4);
                        if (resolvedPath != null)
                        {
                            return node.CreateBindingSuccess(resolvedPath);
                        }

                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest());
                    }
                }
                else if (path[3] == "//")
                {
                    if (RequestNode.BodyNode is null)
                    {
                        throw new NotImplementedException();
                        //. return node.CreateBindingFailure();
                    }
                    var xmlDoc = new XPathDocument(RequestNode.BodyNode.Text);
                    var nav = xmlDoc.CreateNavigator();

                    //Remove the leading slash
                    var xmlNode = nav.SelectSingleNode(path[3].Substring(1));
                    if (xmlNode is not null)
                    {
                        return node.CreateBindingSuccess(xmlNode?.Value);
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //return node.CreateBindingFailure(xmlNode?.Value);
                    }
                }
            }
            else if (path[2] == "headers")
            {
                if (RequestNode.HeadersNode is null)
                {
                    throw new NotImplementedException();
                }

                var headerNode = RequestNode.HeadersNode.HeaderNodes.First(hn => hn.NameNode?.Text == path[3]);

                if (headerNode is null || headerNode.ValueNode is null)
                {
                    throw new NotImplementedException();
                }

                return node.CreateBindingSuccess(headerNode.ValueNode.Text);
            }
        }
        else if (path[1] == "response")
        {
            if (path[2] == "body")
            {
                if (path[3] == "$")
                {
                    if (Response.Content is null)
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest());
                    }

                    var responseJSON = JsonNode.Parse(Response.Content.Raw);

                    if (responseJSON is not null)
                    {
                        var resolvedPath = ResolveJsonPath(responseJSON, path, 4);
                        if (resolvedPath != null)
                        {
                            return node.CreateBindingSuccess(resolvedPath);
                        }

                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest());
                    }
                }
                else if (path[3] == "//")
                {
                    if(Response.Content is null)
                    {
                        throw new NotImplementedException();
                       //. return node.CreateBindingFailure();
                    }
                    var xmlDoc = new XPathDocument(Response.Content.Raw);
                    var nav = xmlDoc.CreateNavigator();

                    //Remove the leading slash
                    var xmlNode = nav.SelectSingleNode(path[3].Substring(1));
                    if (xmlNode is not null)
                    {
                        return node.CreateBindingSuccess(xmlNode?.Value);
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //return node.CreateBindingFailure(xmlNode?.Value);
                    }
                    
                }
            }
            else if (path[2] == "headers")
            {
                 
                if (Response.Headers.TryGetValue(path[3], out var header) && header is not null)
                {   
                    return node.CreateBindingSuccess(header);
                }
                else
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidHeaderNameInNamedRequest(path[3]));
                }

            }
        }
    
        

        return node.CreateBindingFailure(HttpDiagnostics.InvalidNamedRequestPath(node.Text));
    }

    private object? ResolveJsonPath(JsonNode responseJSON, string[] path, int currentIndex)
    {
        if(currentIndex + 1 == path.Length)
        {
            var result = responseJSON[path[currentIndex]];
            return result?.ToJsonString();
        }

        var newResponseJSON = responseJSON[path[currentIndex + 1]];
        if(newResponseJSON is null)
        {
            return null; 
        }
        else
        {
            return ResolveJsonPath(newResponseJSON, path, currentIndex + 1);
        }
  
    }

    private void HandleFirst()
    {

    }

    private void HandleSecond()
    {

    }

    private void HandleThird()
    {

    }

    private void HandleFour()
    {

    }

}
