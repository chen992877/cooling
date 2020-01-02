
require "Common/define"
require "Common/protocal"
require "Common/functions"
Event = require 'events'

require "3rd/pblua/login_pb"
require "3rd/pbc/protobuf"
local json = require "cjson"

local sproto = require "3rd/sproto/sproto"
local core = require "sproto.core"
local print_r = require "3rd/sproto/print_r"

Network = {};
local this = Network;

local transform;
local gameObject;
local islogging = false;

function Network.Start() 
    
end

--Socket消息--
function Network.ReceiveMsg(msg)
    print(msg)
    for i, v in pairs(json.decode(msg)) do
        print(i .. "------" .. v)
    end
end

function Network.SendMsg(msg)
    networkMgr:SendMsg(msg);
end

--卸载网络监听--
function Network.Unload()
end