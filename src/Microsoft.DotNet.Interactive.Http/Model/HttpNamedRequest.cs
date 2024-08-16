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
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    private readonly HttpRequestNode RequestNode;

    private readonly HttpResponse Response;

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
                if (RequestNode.BodyNode is null)
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidBodyInNamedRequest(path[0]));
                }
                else
                {
                    if (path[3] == "*")
                    {
                        return node.CreateBindingSuccess(RequestNode.BodyNode.Text);
                    }
                    else if (path[3] == "$")
                    {
                        try
                        {
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
                                return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                            }
                        }
                        catch (JsonException)
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                        }

                    }
                    else if (path[3].StartsWith("//"))
                    {
                        try
                        {
                            var xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(RequestNode.BodyNode.Text);

                            var xmlNodes = xmlDoc.SelectNodes(path[3].Substring(1));

                            if (xmlNodes is { Count: 1 })
                            {
                                return node.CreateBindingSuccess(xmlNodes.Item(0)?.Value);
                            }
                            else
                            {
                                return node.CreateBindingFailure(HttpDiagnostics.InvalidXmlNodeInNamedRequest(path[3]));
                            }
                            
                        }
                        catch (XmlException)
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                        }

                    }
                }
            }
            else if (path[2] == "headers")
            {
                if (RequestNode.HeadersNode is null)
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidHeadersInNamedRequest(path[0]));
                }

                var headerNode = RequestNode.HeadersNode.HeaderNodes.FirstOrDefault(hn => hn.NameNode?.Text == path[3]);

                if (headerNode is null || headerNode.ValueNode is null)
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidHeaderNameInNamedRequest(path[3]));
                }

                return node.CreateBindingSuccess(headerNode.ValueNode.Text);
            }
        }
        else if (path[1] == "response")
        {
            if (path[2] == "body")
            {
                if (Response.Content is null)
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                }
                else
                {
                    if (path[3] == "*")
                    {
                        return node.CreateBindingSuccess(Response.Content.Raw);
                    }
                    else if (path[3] == "$")
                    {

                        if (Response.Content.ContentType == null || !(Response.Content.ContentType.StartsWith("application/json")))
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentType(Response.Content.ContentType ?? "null", "application/json"));
                        }

                        try
                        {
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
                                return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                            }
                        }
                        catch (JsonException)
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                        }


                    }
                    else if (path[3].StartsWith("//"))
                    {
                        if (Response.Content is null)
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                        }

                        if (Response.Content.ContentType != "application/xml")
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentType(Response.Content.ContentType ?? "null", "application/xml"));
                        }

                        try
                        {
                            var xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(Response.Content.Raw);

                            //Remove the leading slash
                            var xmlNodes = xmlDoc.SelectNodes(path[3].Substring(1));

                            if (xmlNodes is { Count: 1 })
                            {
                                return node.CreateBindingSuccess(xmlNodes.Item(0)?.Value);
                            }
                            else
                            {
                                return node.CreateBindingFailure(HttpDiagnostics.InvalidXmlNodeInNamedRequest(path[3]));
                            }
                        }
                        catch (XmlException)
                        {
                            return node.CreateBindingFailure(HttpDiagnostics.InvalidContentInNamedRequest(string.Join(".", path)));
                        }
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

    private static object? ResolveJsonPath(JsonNode responseJSON, string[] path, int currentIndex)
    {
        if (currentIndex + 1 == path.Length)
        {
            if(responseJSON.GetType() == typeof(JsonArray))
            {
                var jsonArray = (JsonArray)responseJSON;
                return jsonArray.FirstOrDefault(n => n?[path[currentIndex]] != null)?.ToJsonString();
            } 
            else if(responseJSON.GetType() == typeof(JsonObject))
            {
                var jsonObject = (JsonObject)responseJSON;
                return jsonObject[path[currentIndex]]?.ToString();
            }
            else
            {
                return null;
            }
        }

        JsonNode? newResponseJSON = null;
        try
        {
            newResponseJSON = responseJSON[path[currentIndex]];
        } 
        catch (Exception)
        {
            return null;
        }
        
        if (newResponseJSON is null)
        {
            return null;
        }
        else
        {
            return ResolveJsonPath(newResponseJSON, path, currentIndex + 1);
        }

    }
}
