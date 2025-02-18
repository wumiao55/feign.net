﻿using Feign.Formatting;
using Feign.Internal;
using Feign.Pipeline.Internal;
using Feign.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Feign.Proxy
{
    partial class FeignClientHttpProxy<TService>
    {
        /// <summary>
        /// 获取响应结果
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="responseContext"></param>
        /// <returns></returns>
        private async Task<TResult> GetResultAsync<TResult>(ResponsePipelineContext<TService, TResult> responseContext)
        {
            if (responseContext.ResponseMessage == null)
            {
                return default;
            }

            #region ReceivingResponse
            await OnReceivingResponseAsync(responseContext)
#if CONFIGUREAWAIT_FALSE
               .ConfigureAwait(false)
#endif
               ;
            //if (receivingResponseContext.Result != null)
            if (responseContext._isSetResult)
            {
                await OnReceivedResponseAsync(responseContext)
#if CONFIGUREAWAIT_FALSE
                    .ConfigureAwait(false)
#endif
                    ;
                return responseContext.GetResult();
            }
            #endregion

            var result = await GetResultInternalAsync<TResult>(responseContext.Request, responseContext.ResponseMessage)
#if CONFIGUREAWAIT_FALSE
                .ConfigureAwait(false)
#endif
                ;
            responseContext._result = result;
            await OnReceivedResponseAsync(responseContext)
#if CONFIGUREAWAIT_FALSE
                .ConfigureAwait(false)
#endif
                ;
            return result;
        }

        private async Task<TResult> GetResultInternalAsync<TResult>(FeignClientHttpRequest request, HttpResponseMessage responseMessage)
        {
            await EnsureSuccessAsync(request, responseMessage)
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
                ;

            var specialResult = await SpecialResults.GetSpecialResultAsync<TResult>(responseMessage)
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
                 ;
            if (specialResult.IsSpecialResult)
            {
                return specialResult.Result;
            }

            if (responseMessage.Content.Headers.ContentType == null && responseMessage.Content.Headers.ContentLength == 0)
            {
                return default(TResult);
            }
            IMediaTypeFormatter mediaTypeFormatter = FeignOptions.MediaTypeFormatters.FindFormatter(responseMessage.Content.Headers.ContentType?.MediaType);
            if (mediaTypeFormatter == null)
            {
                throw new FeignHttpRequestException(this,
                     responseMessage.RequestMessage as FeignHttpRequestMessage,
                     new HttpRequestException($"Content type '{responseMessage.Content.Headers.ContentType?.ToString()}' not supported"));
            }

            using (var stream = await responseMessage.Content.ReadAsStreamAsync()
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
           )
            {
                if (stream.CanSeek)
                {
                    return await GetResultAsyncInternal<TResult>(mediaTypeFormatter, stream, responseMessage.Content.Headers.ContentType, request.Method.ResultType)
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
                        ;
                }
                using (Stream seekStream = new MemoryStream())
                {
                    await stream.CopyToAsync(seekStream)
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
                        ;
                    seekStream.Seek(0, SeekOrigin.Begin);
                    return await GetResultAsyncInternal<TResult>(mediaTypeFormatter, seekStream, responseMessage.Content.Headers.ContentType, request.Method.ResultType)
#if CONFIGUREAWAIT_FALSE
           .ConfigureAwait(false)
#endif
           ;
                }
            }

        }

        private Task<TResult> GetResultAsyncInternal<TResult>(IMediaTypeFormatter mediaTypeFormatter, Stream stream, MediaTypeHeaderValue mediaTypeHeaderValue, Type resultType)
        {
            if (resultType != null)
            {
                return mediaTypeFormatter.GetResultAsync<TResult>(resultType, stream, FeignClientUtils.GetEncoding(mediaTypeHeaderValue));
            }
            return mediaTypeFormatter.GetResultAsync<TResult>(stream, FeignClientUtils.GetEncoding(mediaTypeHeaderValue));
        }


    }
}
