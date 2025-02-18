﻿using Feign.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Feign.Pipeline.Internal
{
    internal partial class ResponsePipelineContext<TService, TResult> : FeignClientPipelineContext<TService>,
        IReceivedResponsePipelineContext<TService>,
        IReceivingResponsePipelineContext<TService>
    {
        public ResponsePipelineContext(
            IFeignClient<TService> feignClient,
            FeignClientHttpRequest request,
            HttpResponseMessage responseMessage
            ) : base(feignClient)
        {
            Request = request;
            ResponseMessage = responseMessage;
        }

        public FeignClientHttpRequest Request { get; }


        #region ReceivingResponse and ReceivingResponse

        public HttpResponseMessage ResponseMessage { get; set; }

        /// <summary>
        /// 获取返回的类型
        /// </summary>
        public Type ResultType => typeof(TResult);

        internal bool _isSetResult;

        internal TResult _result;
        /// <summary>
        /// 获取或设置返回对象
        /// </summary>
        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                _isSetResult = true;
                _result = (TResult)value;
            }
        }

        internal TResult GetResult()
        {
            return _result;
        }
        #endregion



    }

}
